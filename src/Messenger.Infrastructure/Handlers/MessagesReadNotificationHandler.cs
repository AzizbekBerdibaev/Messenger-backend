using MediatR;
using Messenger.Application.Features.Messages.Notifications;
using Messenger.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Handlers;

/// <summary>
/// MediatR notification handler that notifies chat participants via SignalR
/// when messages are marked as read (<see cref="MessagesReadNotification"/>).
/// </summary>
public sealed class MessagesReadNotificationHandler
    : INotificationHandler<MessagesReadNotification>
{
    private readonly IHubContext<ChatHub, IChatHubClient> _hubContext;
    private readonly ILogger<MessagesReadNotificationHandler> _logger;

    public MessagesReadNotificationHandler(
        IHubContext<ChatHub, IChatHubClient>     hubContext,
        ILogger<MessagesReadNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _logger     = logger;
    }

    public async Task Handle(
        MessagesReadNotification notification,
        CancellationToken        cancellationToken)
    {
        var dto = new ChatHubReadReceiptDto(
            ChatId:        notification.ChatId,
            ReaderId:      notification.ReaderId,
            UpToMessageId: notification.UpToMessageId);

        var groupName = ChatHub.ChatGroupName(notification.ChatId);

        await _hubContext.Clients
            .Group(groupName)
            .MessagesRead(dto);

        _logger.LogDebug(
            "Pushed read receipt for chat {ChatId} by user {ReaderId} via SignalR",
            notification.ChatId, notification.ReaderId);
    }
}
