using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Stories.Queries.GetActiveStories;

/// <summary>
/// Handles <see cref="GetActiveStoriesQuery"/>.
/// Returns active stories from contacts (users sharing at least one chat).
/// Stories are ordered newest-first.
/// </summary>
public sealed class GetActiveStoriesQueryHandler
    : IRequestHandler<GetActiveStoriesQuery, List<ActiveStoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetActiveStoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ActiveStoryDto>> Handle(
        GetActiveStoriesQuery query,
        CancellationToken     cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Find all user IDs that share a chat with the requesting user
        var contactIds = await _db.ChatParticipants
            .Where(p => p.Chat!.Participants.Any(cp => cp.UserId == query.UserId))
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Fetch active stories from contacts (including the user themselves)
        var stories = await _db.Stories
            .Where(s => s.ExpiresAt > now)
            .Where(s => contactIds.Contains(s.UserId))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                AuthorFirstName = s.User!.FirstName,
                AuthorAvatarUrl = s.User.AvatarUrl,
                s.MediaUrl,
                s.CreatedAt,
                s.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return stories.Select(s => new ActiveStoryDto(
            s.Id,
            s.UserId,
            s.AuthorFirstName,
            s.AuthorAvatarUrl,
            s.MediaUrl,
            s.CreatedAt,
            s.ExpiresAt)).ToList();
    }
}
