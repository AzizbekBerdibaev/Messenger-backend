using FluentAssertions;
using Messenger.Application.Features.Chats.Queries.GetChatMessages;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Chats;

/// <summary>
/// Unit tests for <see cref="GetChatMessagesQueryHandler"/>.
/// Note: Tests that require EF navigation properties (Sender, Statuses)
/// are covered by integration tests instead.
/// </summary>
public sealed class GetChatMessagesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();

    private GetChatMessagesQueryHandler CreateHandler() => new(_dbMock.Object);

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 1: User is not a member — throws UnauthorizedAccessException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "User not in chat throws UnauthorizedAccessException")]
    public async Task Handle_UserNotInChat_ThrowsUnauthorized()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetChatMessagesQuery(chatId, userId, null, 50);

        // Empty participant set — user is not a member
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>([]);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);

        // ── Act & Assert ──────────────────────────────────────────────────────
        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(query, CancellationToken.None));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 2: User is a member but chat has no messages — returns empty result
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Member in chat with no messages returns empty list with null NextCursor")]
    public async Task Handle_MemberNoMessages_ReturnsEmpty()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetChatMessagesQuery(chatId, userId, null, 50);

        var participant = ChatParticipant.Create(chatId, userId);
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>([participant]);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);

        // Empty messages set — no Sender navigation property needed
        var messageSet = TestDbSetHelper.CreateMockDbSet<Message>([]);
        _dbMock.Setup(d => d.Messages).Returns(messageSet);

        // ── Act ───────────────────────────────────────────────────────────────
        var handler = CreateHandler();
        var result = await handler.Handle(query, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        result.Messages.Should().BeEmpty();
        result.NextCursor.Should().BeNull();
    }
}
