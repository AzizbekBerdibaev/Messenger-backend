using FluentAssertions;
using Messenger.Application.Features.Chats.Commands.CreatePrivateChat;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Chats;

/// <summary>
/// Unit tests for <see cref="CreatePrivateChatCommandHandler"/>.
/// 
/// Note: The idempotency scenario (existing private chat is returned) requires
/// EF navigation properties (Chat.Participants) and is covered by integration tests.
/// </summary>
public sealed class CreatePrivateChatCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly Mock<ILogger<CreatePrivateChatCommandHandler>> _loggerMock = new();

    private CreatePrivateChatCommandHandler CreateHandler() => new(
        _dbMock.Object,
        _loggerMock.Object);

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 1: Chat does not exist, OtherUser exists → creates new chat
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "No existing chat creates a new private chat with two participants")]
    public async Task Handle_NoExistingChat_CreatesNewPrivateChat()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var command = new CreatePrivateChatCommand(userA, userB);

        // No existing chats, userB exists
        var chatSet = TestDbSetHelper.CreateMockDbSet<Chat>([]);
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>([]);
        var userB_entity = User.Create(userB, "+79991234567");
        var usersSet = TestDbSetHelper.CreateMockDbSet<User>([userB_entity]);

        _dbMock.Setup(d => d.Chats).Returns(chatSet);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);
        _dbMock.Setup(d => d.Users).Returns(usersSet);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // ── Act ───────────────────────────────────────────────────────────────
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        result.AlreadyExisted.Should().BeFalse();
        result.ChatId.Should().NotBeEmpty();

        _dbMock.Verify(d => d.Chats.Add(It.Is<Chat>(c => c.Type == ChatType.Private)), Times.Once);
        _dbMock.Verify(d => d.ChatParticipants.Add(It.IsAny<ChatParticipant>()), Times.Exactly(2));
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 2: OtherUser does not exist — throws KeyNotFoundException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "OtherUser not found throws KeyNotFoundException")]
    public async Task Handle_OtherUserNotFound_ThrowsKeyNotFoundException()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var command = new CreatePrivateChatCommand(userA, userB);

        var chatSet = TestDbSetHelper.CreateMockDbSet<Chat>([]);
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>([]);
        var usersSet = TestDbSetHelper.CreateMockDbSet<User>([]);   // empty — userB not found

        _dbMock.Setup(d => d.Chats).Returns(chatSet);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);
        _dbMock.Setup(d => d.Users).Returns(usersSet);

        // ── Act & Assert ──────────────────────────────────────────────────────
        var handler = CreateHandler();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));

        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
