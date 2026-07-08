using MediatR;

namespace Messenger.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command: Rotates the JWT token pair using a valid Refresh Token.
/// The old refresh token is validated; on success a new pair is issued.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;

/// <summary>Result containing the new token pair.</summary>
public sealed record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);
