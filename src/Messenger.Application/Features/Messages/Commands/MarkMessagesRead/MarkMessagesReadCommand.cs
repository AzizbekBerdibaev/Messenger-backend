using MediatR;

namespace Messenger.Application.Features.Messages.Commands.MarkMessagesRead;

/// <summary>
/// Command: Marks all messages in a chat up to and including
/// <see cref="UpToMessageId"/> as <c>Read</c> for the current user.
/// After persisting, fires <c>MessagesReadNotification</c> for SignalR.
/// </summary>
public sealed record MarkMessagesReadCommand(
    Guid ChatId,
    Guid UserId,
    Guid UpToMessageId) : IRequest<Unit>;
