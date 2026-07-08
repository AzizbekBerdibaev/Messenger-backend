using MediatR;
using Messenger.Application.Features.Messages.Notifications;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Messages.Commands.SendMessage;

/// <summary>
/// Handles <see cref="SendMessageCommand"/>.
/// Steps:
/// 1. Verify sender is a member of the chat.
/// 2. Create the <see cref="Message"/> via the domain factory method.
/// 3. Create <see cref="MessageStatus"/> (Sent) for every recipient.
/// 4. Persist all changes atomically.
/// 5. Publish <see cref="MessageSentNotification"/> for real-time delivery.
/// </summary>
public sealed class SendMessageCommandHandler
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IApplicationDbContext db,
        IPublisher publisher,
        ILogger<SendMessageCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<SendMessageResult> Handle(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        // ── Step 1: Verify membership ──────────────────────────────────────────
        var participants = await _db.ChatParticipants
            .Where(p => p.ChatId == command.ChatId)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        if (!participants.Contains(command.SenderId))
        {
            throw new UnauthorizedAccessException(
                $"User {command.SenderId} is not a member of chat {command.ChatId}.");
        }

        // Check if any participant has blocked the sender
        var isBlocked = await _db.BlockedUsers
            .AnyAsync(b => participants.Contains(b.UserId) && b.BlockedUserId == command.SenderId, cancellationToken);

        if (isBlocked)
        {
            throw new UnauthorizedAccessException("You have been blocked by a participant in this chat.");
        }

        // ── Step 2: Create message via domain method ───────────────────────────
        Message message = command.Type switch
        {
            MessageType.Text => Message.CreateText(
                command.ChatId, command.SenderId,
                command.Content!, command.ReplyToMessageId),
            MessageType.Image => Message.CreateImage(
                command.ChatId, command.SenderId,
                command.MediaUrl!, command.Content, command.ReplyToMessageId),
            MessageType.Audio => Message.CreateAudio(
                command.ChatId, command.SenderId,
                command.MediaUrl!, command.Duration, command.ReplyToMessageId),
            _ => throw new InvalidOperationException($"Unsupported message type: {command.Type}")
        };

        _db.Messages.Add(message);

        // ── Step 3: Create MessageStatus for each recipient ───────────────────
        var recipientIds = participants
            .Where(id => id != command.SenderId)
            .ToList();

        foreach (var recipientId in recipientIds)
        {
            _db.MessageStatuses.Add(
                MessageStatus.CreateSent(message.Id, recipientId));
        }

        // ── Step 4: Persist ────────────────────────────────────────────────────
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Message {MessageId} sent to chat {ChatId} by user {SenderId}",
            message.Id, command.ChatId, command.SenderId);

        // ── Step 5: Publish notification for SignalR ──────────────────────────
        await _publisher.Publish(
            new MessageSentNotification(message, participants),
            cancellationToken);

        return new SendMessageResult(
            MessageId: message.Id,
            ChatId: message.ChatId,
            SenderId: message.SenderId,
            Type: message.Type,
            Content: message.Content,
            MediaUrl: message.MediaUrl,
            Duration: message.Duration,
            ReplyToMessageId: message.ReplyToMessageId,
            CreatedAt: message.CreatedAt);
    }
}
