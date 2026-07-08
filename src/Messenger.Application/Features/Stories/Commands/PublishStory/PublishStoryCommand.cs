using MediatR;

namespace Messenger.Application.Features.Stories.Commands.PublishStory;

/// <summary>
/// Command: Publishes a new 24-hour story for the current user.
/// </summary>
public sealed record PublishStoryCommand(
    Guid UserId,
    string MediaUrl) : IRequest<StoryDto>;

/// <summary>DTO returned after story creation.</summary>
public sealed record StoryDto(
    Guid StoryId,
    Guid UserId,
    string MediaUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt);
