using MediatR;

namespace Messenger.Application.Features.Profile.Commands.UpdateProfile;

/// <summary>
/// Command: Updates the authenticated user's display name and optional avatar.
/// <see cref="UserId"/> is populated from the JWT claims in the controller — never from the request body.
/// </summary>
public sealed record UpdateProfileCommand(
    Guid    UserId,
    string  FirstName,
    string? LastName,
    string? AvatarUrl) : IRequest<UpdateProfileResult>;

/// <summary>Snapshot of the profile after the update.</summary>
public sealed record UpdateProfileResult(
    Guid    UserId,
    string  FirstName,
    string? LastName,
    string? AvatarUrl);
