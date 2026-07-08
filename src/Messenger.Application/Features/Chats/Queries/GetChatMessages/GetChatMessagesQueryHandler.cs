using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Chats.Queries.GetChatMessages;

/// <summary>
/// Handles <see cref="GetChatMessagesQuery"/>.
/// Returns messages newest-first with cursor-based pagination.
/// Throws <see cref="UnauthorizedAccessException"/> if the user is not a chat participant.
/// </summary>
public sealed class GetChatMessagesQueryHandler
    : IRequestHandler<GetChatMessagesQuery, GetChatMessagesResult>
{
    private readonly IApplicationDbContext _db;

    public GetChatMessagesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<GetChatMessagesResult> Handle(
        GetChatMessagesQuery query,
        CancellationToken cancellationToken)
    {
        // ── Guard: user must be a participant ─────────────────────────────────
        var isMember = await _db.ChatParticipants
            .AnyAsync(p => p.ChatId == query.ChatId && p.UserId == query.UserId,
                cancellationToken);

        if (!isMember)
        {
            throw new UnauthorizedAccessException(
                $"User {query.UserId} is not a member of chat {query.ChatId}.");
        }

        var limit = Math.Clamp(query.Limit, 1, 100);

        // ── Fetch messages with cursor ─────────────────────────────────────────
        var messagesQuery = _db.Messages
            .Where(m => m.ChatId == query.ChatId);

        if (query.BeforeCursor.HasValue)
        {
            messagesQuery = messagesQuery
                .Where(m => m.CreatedAt < query.BeforeCursor.Value);
        }

        // Fetch limit + 1 to determine if there is a next page
        var rawMessages = await messagesQuery
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit + 1)
            .Select(m => new
            {
                m.Id,
                m.ChatId,
                m.SenderId,
                SenderFirstName = m.Sender!.FirstName,
                SenderAvatarUrl = m.Sender.AvatarUrl,
                m.Type,
                m.Content,
                m.MediaUrl,
                m.Duration,
                m.ReplyToMessageId,
                m.CreatedAt,
                MyStatus = m.Statuses
                    .Where(s => s.UserId == query.UserId)
                    .Select(s => (Domain.Enums.DeliveryStatus?)s.Status)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var hasMore = rawMessages.Count > limit;
        var pageItems = hasMore ? rawMessages.Take(limit).ToList() : rawMessages;

        var dtos = pageItems.Select(m => new MessageDto(
            MessageId: m.Id,
            ChatId: m.ChatId,
            SenderId: m.SenderId,
            SenderFirstName: m.SenderFirstName,
            SenderAvatarUrl: m.SenderAvatarUrl,
            Type: m.Type,
            Content: m.Content,
            MediaUrl: m.MediaUrl,
            Duration: m.Duration,
            ReplyToMessageId: m.ReplyToMessageId,
            CreatedAt: m.CreatedAt,
            MyStatus: m.MyStatus
        )).ToList();

        var nextCursor = hasMore
            ? pageItems.Last().CreatedAt
            : (DateTime?)null;

        return new GetChatMessagesResult(dtos, nextCursor);
    }
}
