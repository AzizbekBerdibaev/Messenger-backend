using MediatR;
using Messenger.Application.Features.Messages.Notifications;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Messages.Commands.MarkMessagesRead;

/// <summary>
/// Handles <see cref="MarkMessagesReadCommand"/>.
/// Steps:
/// 1. Find all message statuses for the user up to the target message.
/// 2. Call <c>MarkRead()</c> on each status.
/// 3. Persist and publish <see cref="MessagesReadNotification"/>.
/// </summary>
public sealed class MarkMessagesReadCommandHandler
    : IRequestHandler<MarkMessagesReadCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<MarkMessagesReadCommandHandler> _logger;

    public MarkMessagesReadCommandHandler(
        IApplicationDbContext db,
        IPublisher publisher,
        ILogger<MarkMessagesReadCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Unit> Handle(
        MarkMessagesReadCommand command,
        CancellationToken cancellationToken)
    {
        // Find the target message's timestamp for range filtering
        var targetMessage = await _db.Messages
            .Where(m => m.Id == command.UpToMessageId && m.ChatId == command.ChatId)
            .Select(m => new { m.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (targetMessage is null)
        {
            throw new KeyNotFoundException(
                $"Message {command.UpToMessageId} not found in chat {command.ChatId}.");
        }

        // Load all unread statuses for this user up to and including the target message
        var statuses = await _db.MessageStatuses
            .Where(s => s.UserId == command.UserId)
            .Where(s => s.Message!.ChatId == command.ChatId)
            .Where(s => s.Message!.CreatedAt <= targetMessage.CreatedAt)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var status in statuses)
        {
            if (status.Status < Domain.Enums.DeliveryStatus.Read)
            {
                status.MarkRead();
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User {UserId} marked {Count} messages as read in chat {ChatId}",
                command.UserId, updatedCount, command.ChatId);

            await _publisher.Publish(
                new MessagesReadNotification(command.ChatId, command.UserId, command.UpToMessageId),
                cancellationToken);
        }

        return Unit.Value;
    }
}
