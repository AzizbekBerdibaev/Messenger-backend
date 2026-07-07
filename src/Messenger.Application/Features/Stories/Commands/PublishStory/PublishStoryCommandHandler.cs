using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Stories.Commands.PublishStory;

/// <summary>
/// Handles <see cref="PublishStoryCommand"/>.
/// Creates a <see cref="Story"/> with a 24-hour lifetime and persists it.
/// </summary>
public sealed class PublishStoryCommandHandler
    : IRequestHandler<PublishStoryCommand, StoryDto>
{
    private readonly IApplicationDbContext           _db;
    private readonly ILogger<PublishStoryCommandHandler> _logger;

    public PublishStoryCommandHandler(
        IApplicationDbContext            db,
        ILogger<PublishStoryCommandHandler> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<StoryDto> Handle(
        PublishStoryCommand command,
        CancellationToken   cancellationToken)
    {
        var story = Story.Create(command.UserId, command.MediaUrl);
        _db.Stories.Add(story);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} published story {StoryId}, expires at {ExpiresAt}",
            command.UserId, story.Id, story.ExpiresAt);

        return new StoryDto(
            StoryId:   story.Id,
            UserId:    story.UserId,
            MediaUrl:  story.MediaUrl,
            CreatedAt: story.CreatedAt,
            ExpiresAt: story.ExpiresAt);
    }
}
