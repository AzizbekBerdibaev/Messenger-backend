using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bogus;
using FluentAssertions;
using Messenger.Application.Features.Chats.Queries.GetChats;
using Messenger.Domain.Enums;
using Messenger.Infrastructure.Persistence;
using Messenger.IntegrationTests.Infrastructure;
using Messenger.WebApi.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Messenger.IntegrationTests.Chats;

/// <summary>
/// End-to-end integration tests for the chat and messaging flow.
/// Uses real Testcontainers (PostgreSQL + Redis) via <see cref="MessengerWebApplicationFactory"/>.
/// </summary>
[Collection("Integration")]
public sealed class ChatFlowTests : IClassFixture<MessengerWebApplicationFactory>
{
    private readonly MessengerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Faker Faker = new("en");

    public ChatFlowTests(MessengerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: authenticate a user and return access token
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(Guid UserId, string AccessToken)> AuthenticateUserAsync()
    {
        var phone = "+7" + Faker.Random.ReplaceNumbers("##########");

        // Send OTP
        await _client.PostAsJsonAsync("/api/auth/send-otp", new SendOtpRequest(phone));

        // Peek OTP from Redis
        var redis = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var code = (string?)await redis.GetDatabase().StringGetAsync($"otp:{phone}");
        code.Should().NotBeNull();

        // Verify OTP → get tokens
        var resp = await _client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtpRequest(phone, code!));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        body.Should().NotBeNull();

        return (body!.UserId, body.AccessToken);
    }

    private HttpClient CreateClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 1: Full chat flow — create chat, send message, get history
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create private chat → send text message → retrieve message in history")]
    public async Task FullChatFlow_CreateSendRetrieve()
    {
        // ── Arrange: authenticate two users ──────────────────────────────────
        var (userAId, tokenA) = await AuthenticateUserAsync();
        var (userBId, tokenB) = await AuthenticateUserAsync();

        var clientA = CreateClientWithToken(tokenA);
        var clientB = CreateClientWithToken(tokenB);

        // ── Act 1: User A creates private chat with User B ────────────────────
        var createResp = await clientA.PostAsJsonAsync(
            "/api/chats/private",
            new CreatePrivateChatRequest(userBId));

        createResp.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "creating a private chat should succeed");

        var chatBody = await createResp.Content.ReadFromJsonAsync<CreatePrivateChatResponse>();
        chatBody.Should().NotBeNull();
        var chatId = chatBody!.ChatId;

        // ── Act 2: Create the same chat again — should be idempotent ─────────
        var createResp2 = await clientA.PostAsJsonAsync(
            "/api/chats/private",
            new CreatePrivateChatRequest(userBId));

        createResp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var chatBody2 = await createResp2.Content.ReadFromJsonAsync<CreatePrivateChatResponse>();
        chatBody2!.ChatId.Should().Be(chatId,
            because: "second call must return the same chat");
        chatBody2.AlreadyExisted.Should().BeTrue();

        // ── Act 3: User A sends a text message ────────────────────────────────
        var sendResp = await clientA.PostAsJsonAsync(
            $"/api/chats/{chatId}/messages",
            new SendMessageRequest(MessageType.Text, "Hello from A!", null, null, null));

        sendResp.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "sending a valid text message should return 201");

        var msgBody = await sendResp.Content.ReadFromJsonAsync<SendMessageResponse>();
        msgBody.Should().NotBeNull();
        msgBody!.Content.Should().Be("Hello from A!");
        msgBody.ChatId.Should().Be(chatId);
        msgBody.SenderId.Should().Be(userAId);

        // ── Assert 4: Message appears in User B's chat history ────────────────
        var historyResp = await clientB.GetAsync($"/api/chats/{chatId}/messages");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyBody = await historyResp.Content.ReadFromJsonAsync<GetChatMessagesResponse>();
        historyBody.Should().NotBeNull();
        historyBody!.Messages.Should().ContainSingle(m =>
            m.Content == "Hello from A!" &&
            m.SenderId == userAId);

        // ── Assert 5: Chat appears in both users' chat lists ──────────────────
        var listRespA = await clientA.GetAsync("/api/chats");
        listRespA.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBodyA = await listRespA.Content.ReadFromJsonAsync<List<ChatSummaryDto>>();
        listBodyA.Should().NotBeNull();
        listBodyA.Should().Contain(c => c.ChatId == chatId);

        // ── Assert 6: Message persisted in DB ─────────────────────────────────
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbMsg = await db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == msgBody.MessageId);

        dbMsg.Should().NotBeNull();
        dbMsg!.Content.Should().Be("Hello from A!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 2: Non-member cannot send message → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Non-member cannot send message to a chat — returns 401")]
    public async Task SendMessage_NonMember_Returns401()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var (userAId, tokenA) = await AuthenticateUserAsync();
        var (userBId, tokenB) = await AuthenticateUserAsync();
        var (userCId, tokenC) = await AuthenticateUserAsync();

        var clientA = CreateClientWithToken(tokenA);
        var clientC = CreateClientWithToken(tokenC);

        // A creates private chat with B (C is not a member)
        var createResp = await clientA.PostAsJsonAsync(
            "/api/chats/private",
            new CreatePrivateChatRequest(userBId));
        var chatBody = await createResp.Content.ReadFromJsonAsync<CreatePrivateChatResponse>();
        var chatId = chatBody!.ChatId;

        // ── Act: C tries to send a message ────────────────────────────────────
        var sendResp = await clientC.PostAsJsonAsync(
            $"/api/chats/{chatId}/messages",
            new SendMessageRequest(MessageType.Text, "I'm not a member!", null, null, null));

        // ── Assert ────────────────────────────────────────────────────────────
        sendResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "non-members must not be able to send messages");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 3: Mark messages as read → 204
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Mark messages read returns 204 and updates MessageStatus in DB")]
    public async Task MarkMessagesRead_Updates_Status()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var (userAId, tokenA) = await AuthenticateUserAsync();
        var (userBId, tokenB) = await AuthenticateUserAsync();

        var clientA = CreateClientWithToken(tokenA);
        var clientB = CreateClientWithToken(tokenB);

        // Create chat
        var chatResp = await clientA.PostAsJsonAsync(
            "/api/chats/private", new CreatePrivateChatRequest(userBId));
        var chatBody = await chatResp.Content.ReadFromJsonAsync<CreatePrivateChatResponse>();
        var chatId = chatBody!.ChatId;

        // A sends a message
        var sendResp = await clientA.PostAsJsonAsync(
            $"/api/chats/{chatId}/messages",
            new SendMessageRequest(MessageType.Text, "Read me!", null, null, null));
        var msgBody = await sendResp.Content.ReadFromJsonAsync<SendMessageResponse>();

        // ── Act: B marks the message as read ──────────────────────────────────
        var readResp = await clientB.PutAsJsonAsync(
            $"/api/chats/{chatId}/messages/read",
            new MarkReadRequest(msgBody!.MessageId));

        // ── Assert ────────────────────────────────────────────────────────────
        readResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var statusInDb = await db.MessageStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.MessageId == msgBody.MessageId &&
                s.UserId == userBId);

        statusInDb.Should().NotBeNull();
        statusInDb!.Status.Should().Be(DeliveryStatus.Read);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 4: Group chat creation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create group chat with multiple participants")]
    public async Task CreateGroupChat_WithMultipleParticipants_ReturnsCorrectCount()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var (userAId, tokenA) = await AuthenticateUserAsync();
        var (userBId, _) = await AuthenticateUserAsync();
        var (userCId, _) = await AuthenticateUserAsync();

        var clientA = CreateClientWithToken(tokenA);

        // ── Act ───────────────────────────────────────────────────────────────
        var createResp = await clientA.PostAsJsonAsync(
            "/api/chats/group",
            new CreateGroupChatRequest([userBId, userCId]));

        // ── Assert ────────────────────────────────────────────────────────────
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var chatBody = await createResp.Content.ReadFromJsonAsync<CreateGroupChatResponse>();
        chatBody.Should().NotBeNull();
        chatBody!.ParticipantCount.Should().Be(3,
            because: "creator + 2 participants = 3 total");
    }
}
