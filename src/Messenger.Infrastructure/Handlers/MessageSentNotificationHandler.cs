using MediatR;
using Messenger.Application.Features.Messages.Notifications;
using Messenger.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Messenger.Domain.Interfaces;

namespace Messenger.Infrastructure.Handlers;

/// <summary>
/// MediatR notification handler that pushes a new message to all chat participants
/// via SignalR when <see cref="MessageSentNotification"/> is published.
/// </summary>
public sealed class MessageSentNotificationHandler
    : INotificationHandler<MessageSentNotification>
{
    private readonly IHubContext<ChatHub, IChatHubClient> _hubContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<MessageSentNotificationHandler> _logger;

    public MessageSentNotificationHandler(
        IHubContext<ChatHub, IChatHubClient> hubContext,
        IApplicationDbContext dbContext,
        IPushNotificationService pushService,
        ILogger<MessageSentNotificationHandler> logger)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Handle(
        MessageSentNotification notification,
        CancellationToken cancellationToken)
    {
        var msg = notification.Message;

        var dto = new ChatHubMessageDto(
            MessageId: msg.Id,
            ChatId: msg.ChatId,
            SenderId: msg.SenderId,
            SenderFirstName: string.Empty,   // resolved on the hub side from client cache
            SenderAvatarUrl: null,
            Type: msg.Type,
            Content: msg.Content,
            MediaUrl: msg.MediaUrl,
            ReplyToMessageId: msg.ReplyToMessageId,
            CreatedAt: msg.CreatedAt);

        var groupName = ChatHub.ChatGroupName(msg.ChatId);

        // Send via SignalR
        await _hubContext.Clients
            .Group(groupName)
            .ReceiveMessage(dto);

        _logger.LogDebug(
            "Pushed message {MessageId} to SignalR group {Group}",
            msg.Id, groupName);

        // Send push notifications to recipients
        var devices = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.UserDevices
                .Where(d => notification.RecipientIds.Contains(d.UserId)),
            cancellationToken);

        foreach (var device in devices)
        {
            var title = "New Message"; // Ideally resolve sender's name
            var body = string.IsNullOrWhiteSpace(msg.Content) ? "You received an attachment" : msg.Content;

            var data = new Dictionary<string, string>
            {
                { "chatId", msg.ChatId.ToString() },
                { "messageId", msg.Id.ToString() }
            };

            await _pushService.SendNotificationAsync(device.DeviceToken, title, body, data);

            _logger.LogDebug(
                "Sent push notification to {DeviceType} for User {UserId}",
                device.DeviceType, device.UserId);
        }
    }
}
