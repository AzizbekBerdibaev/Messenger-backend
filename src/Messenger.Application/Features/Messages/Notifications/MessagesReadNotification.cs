using MediatR;

namespace Messenger.Application.Features.Messages.Notifications;

/// <summary>
/// Published after one or more messages are marked as read by a user.
/// Consumed by <c>MessagesReadNotificationHandler</c> in Infrastructure
/// to notify the sender via SignalR.
/// </summary>
public sealed record MessagesReadNotification(
    Guid ChatId,
    Guid ReaderId,
    Guid UpToMessageId) : INotification;
