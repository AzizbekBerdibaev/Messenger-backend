using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Stories.Commands.PublishStory;
using Messenger.Application.Features.Stories.Queries.GetActiveStories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

/// <summary>
/// Manages user stories: publish a new story and retrieve active stories from contacts.
/// All endpoints require a valid JWT (Bearer scheme).
/// </summary>
[ApiController]
[Route("api/stories")]
[Authorize]
[Produces("application/json")]
public sealed class StoriesController : ControllerBase
{
    private readonly ISender _sender;

    public StoriesController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Publishes a new 24-hour story for the current user.</summary>
    /// <response code="201">Story published.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishStory(
        [FromBody] PublishStoryRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(
            new PublishStoryCommand(userId, request.MediaUrl), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns active stories from contacts (users sharing a chat with the current user).</summary>
    /// <response code="200">List of active stories, newest first.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ActiveStoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveStories(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(new GetActiveStoriesQuery(userId), ct);
        return Ok(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User identity claim is missing.");

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("User identity claim is not a valid GUID.");
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record PublishStoryRequest(string MediaUrl);
