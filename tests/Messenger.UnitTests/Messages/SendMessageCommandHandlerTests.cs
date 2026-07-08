using FluentAssertions;
using MediatR;
using Messenger.Application.Features.Messages.Commands.SendMessage;
using Messenger.Application.Features.Messages.Notifications;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Messages;

/// <summary>
/// Unit tests for <see cref="SendMessageCommandHandler"/>.
/// </summary>
public sealed class SendMessageCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<SendMessageCommandHandler>> _loggerMock = new();

    private SendMessageCommandHandler CreateHandler() => new(
        _dbMock.Object,
        _publisherMock.Object,
        _loggerMock.Object);

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 1: Successful text message send
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Valid text message is persisted and MessageSentNotification is published")]
    public async Task Handle_ValidTextMessage_PersistsAndPublishesNotification()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var chatId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var command = new SendMessageCommand(
            ChatId: chatId,
            SenderId: senderId,
            Type: MessageType.Text,
            Content: "Hello!",
            MediaUrl: null,
            Duration: null,
            ReplyToMessageId: null);

        // Participants: sender + recipient
        var participants = new List<ChatParticipant>
        {
            ChatParticipant.Create(chatId, senderId),
            ChatParticipant.Create(chatId, otherId)
        };
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>(participants);
        var messagesSet = TestDbSetHelper.CreateMockDbSet<Message>([]);
        var statusSet = TestDbSetHelper.CreateMockDbSet<MessageStatus>([]);
        var blockedSet = TestDbSetHelper.CreateMockDbSet<BlockedUser>([]);

        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);
        _dbMock.Setup(d => d.Messages).Returns(messagesSet);
        _dbMock.Setup(d => d.MessageStatuses).Returns(statusSet);
        _dbMock.Setup(d => d.BlockedUsers).Returns(blockedSet);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisherMock
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ── Act ───────────────────────────────────────────────────────────────
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.ChatId.Should().Be(chatId);
        result.SenderId.Should().Be(senderId);
        result.Content.Should().Be("Hello!");
        result.Type.Should().Be(MessageType.Text);

        // Message was added
        _dbMock.Verify(d => d.Messages.Add(It.Is<Message>(m =>
            m.ChatId == chatId &&
            m.SenderId == senderId &&
            m.Type == MessageType.Text &&
            m.Content == "Hello!")), Times.Once);

        // MessageStatus created for the recipient (not the sender)
        _dbMock.Verify(d => d.MessageStatuses.Add(It.Is<MessageStatus>(s =>
            s.UserId == otherId)), Times.Once);

        // Notification published
        _publisherMock.Verify(p => p.Publish(
            It.IsAny<MessageSentNotification>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 2: Sender not in chat — throws UnauthorizedAccessException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Sender not in chat throws UnauthorizedAccessException")]
    public async Task Handle_SenderNotInChat_ThrowsUnauthorized()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var chatId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var command = new SendMessageCommand(
            chatId, senderId, MessageType.Text, "Hi", null, null, null);

        // Only otherId in the chat — sender is NOT a participant
        var participants = new List<ChatParticipant>
        {
            ChatParticipant.Create(chatId, otherId)
        };
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>(participants);
        var blockedSet = TestDbSetHelper.CreateMockDbSet<BlockedUser>([]);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);
        _dbMock.Setup(d => d.BlockedUsers).Returns(blockedSet);

        // ── Act & Assert ──────────────────────────────────────────────────────
        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(
            It.IsAny<INotification>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 3: Unsupported message type — throws InvalidOperationException
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Unsupported MessageType throws InvalidOperationException")]
    public async Task Handle_UnsupportedMessageType_ThrowsInvalidOperation()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var chatId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        var command = new SendMessageCommand(
            chatId, senderId,
            (MessageType)99,   // invalid enum value
            null, null, null, null);

        var participants = new List<ChatParticipant>
        {
            ChatParticipant.Create(chatId, senderId)
        };
        var participantSet = TestDbSetHelper.CreateMockDbSet<ChatParticipant>(participants);
        var blockedSet = TestDbSetHelper.CreateMockDbSet<BlockedUser>([]);
        _dbMock.Setup(d => d.ChatParticipants).Returns(participantSet);
        _dbMock.Setup(d => d.BlockedUsers).Returns(blockedSet);

        // ── Act & Assert ──────────────────────────────────────────────────────
        var handler = CreateHandler();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
