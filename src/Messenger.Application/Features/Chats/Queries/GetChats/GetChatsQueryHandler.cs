using MediatR;
using Messenger.Domain.Enums;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Chats.Queries.GetChats;

/// <summary>
/// Handles <see cref="GetChatsQuery"/>.
/// Loads all chats for the user, enriched with the last message, participant count,
/// unread count, and private chat details (name, avatar, online status).
/// </summary>
public sealed class GetChatsQueryHandler
    : IRequestHandler<GetChatsQuery, List<ChatSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetChatsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatSummaryDto>> Handle(
        GetChatsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Load all chats the user participates in
        var chats = await _db.Chats
            .Where(c => c.Participants.Any(p => p.UserId == query.UserId))
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.CreatedAt,
                ParticipantCount = c.Participants.Count,

                // For private chats, get the OTHER participant
                OtherParticipant = c.Type == ChatType.Private
                    ? c.Participants
                        .Where(p => p.UserId != query.UserId)
                        .Select(p => new
                        {
                            p.User!.FirstName,
                            p.User.LastName,
                            p.User.AvatarUrl,
                            p.User.LastSeenAt
                        })
                        .FirstOrDefault()
                    : null,

                UnreadCount = c.Messages
                    .SelectMany(m => m.Statuses)
                    .Count(s => s.UserId == query.UserId && s.Status != DeliveryStatus.Read),

                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        SenderFirstName = m.Sender!.FirstName,
                        m.Type,
                        m.Content,
                        m.MediaUrl,
                        m.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .OrderByDescending(c => c.LastMessage != null
                ? c.LastMessage.CreatedAt
                : c.CreatedAt)
            .ToListAsync(cancellationToken);

        return chats.Select(c =>
        {
            // Determine Chat Name, Avatar and Online Status based on chat type
            string? chatName = null;
            string? chatAvatar = null;
            bool isOnline = false;

            if (c.Type == ChatType.Private && c.OtherParticipant != null)
            {
                chatName = string.IsNullOrWhiteSpace(c.OtherParticipant.LastName)
                    ? c.OtherParticipant.FirstName
                    : $"{c.OtherParticipant.FirstName} {c.OtherParticipant.LastName}";

                chatAvatar = c.OtherParticipant.AvatarUrl;
                isOnline = (now - c.OtherParticipant.LastSeenAt).TotalMinutes < 5;
            }
            // For group chats, could be set here if groups had names/avatars

            return new ChatSummaryDto(
                ChatId: c.Id,
                Type: c.Type,
                ParticipantCount: c.ParticipantCount,
                CreatedAt: c.CreatedAt,
                ChatName: chatName,
                ChatAvatarUrl: chatAvatar,
                IsOnline: isOnline,
                UnreadCount: c.UnreadCount,
                LastMessage: c.LastMessage is null ? null : new LastMessageDto(
                    MessageId: c.LastMessage.Id,
                    SenderId: c.LastMessage.SenderId,
                    SenderFirstName: c.LastMessage.SenderFirstName,
                    Type: c.LastMessage.Type,
                    Content: c.LastMessage.Content,
                    MediaUrl: c.LastMessage.MediaUrl,
                    CreatedAt: c.LastMessage.CreatedAt)
            );
        }).ToList();
    }
}
