using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Profile.Commands.UpdateProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

/// <summary>
/// Manages the authenticated user's profile.
/// All endpoints require a valid JWT (Bearer scheme).
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public sealed class ProfileController : ControllerBase
{
    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns the current user's profile information.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(Messenger.Application.Features.Profile.Queries.GetProfile.ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var profile = await _sender.Send(new Messenger.Application.Features.Profile.Queries.GetProfile.GetProfileQuery(userId), ct);
        return Ok(profile);
    }

    /// <summary>Updates the current user's first name, optional last name and optional avatar URL.</summary>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var result = await _sender.Send(
            new UpdateProfileCommand(
                UserId: userId,
                FirstName: request.FirstName,
                LastName: request.LastName,
                AvatarUrl: request.AvatarUrl),
            ct);

        return Ok(new UpdateProfileResponse(
            UserId: result.UserId,
            FirstName: result.FirstName,
            LastName: result.LastName,
            AvatarUrl: result.AvatarUrl));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity claim is missing.");

        return Guid.TryParse(subClaim, out var id)
            ? id
            : throw new UnauthorizedAccessException("User identity claim is not a valid GUID.");
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record UpdateProfileRequest(
    string FirstName,
    string? LastName,
    string? AvatarUrl);

public sealed record UpdateProfileResponse(
    Guid UserId,
    string FirstName,
    string? LastName,
    string? AvatarUrl);
