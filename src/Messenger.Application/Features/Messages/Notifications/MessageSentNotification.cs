using MediatR;
using Messenger.Domain.Entities;

namespace Messenger.Application.Features.Messages.Notifications;

/// <summary>
/// Published after a <see cref="Message"/> is persisted.
/// Consumed by <c>MessageSentNotificationHandler</c> in Infrastructure
/// to push the message to recipients via SignalR.
/// </summary>
public sealed record MessageSentNotification(
    Message Message,
    List<Guid> RecipientIds) : INotification;
