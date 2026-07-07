using MediatR;

namespace Messenger.Application.Features.Stories.Queries.GetActiveStories;

/// <summary>
/// Query: Returns all active (non-expired) stories from users
/// who share at least one chat with the requesting user.
/// </summary>
public sealed record GetActiveStoriesQuery(Guid UserId) : IRequest<List<ActiveStoryDto>>;

/// <summary>DTO representing an active story with author metadata.</summary>
public sealed record ActiveStoryDto(
    Guid     StoryId,
    Guid     AuthorId,
    string   AuthorFirstName,
    string?  AuthorAvatarUrl,
    string   MediaUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt);
