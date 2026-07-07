using MediatR;
using Messenger.Domain.Enums;

namespace Messenger.Application.Features.Messages.Commands.SendMessage;

/// <summary>
/// Command: Sends a message to a chat.
/// The sender must be a member of the chat.
/// After persisting, fires <c>MessageSentNotification</c> for SignalR delivery.
/// </summary>
public sealed record SendMessageCommand(
    Guid         ChatId,
    Guid         SenderId,
    MessageType  Type,
    string?      Content,
    string?      MediaUrl,
    int?         Duration,
    Guid?        ReplyToMessageId) : IRequest<SendMessageResult>;

/// <summary>Result returned after the message is persisted.</summary>
public sealed record SendMessageResult(
    Guid        MessageId,
    Guid        ChatId,
    Guid        SenderId,
    MessageType Type,
    string?     Content,
    string?     MediaUrl,
    int?        Duration,
    Guid?       ReplyToMessageId,
    DateTime    CreatedAt);
