namespace Messenger.Application.Common.Interfaces;

/// <summary>Token pair returned after successful OTP verification or token refresh.</summary>
public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

/// <summary>
/// Abstraction for JWT generation and validation.
/// Implemented in Infrastructure using System.IdentityModel.Tokens.Jwt.
/// </summary>
public interface IJwtService
{
    /// <summary>Generates a short-lived Access Token and a long-lived Refresh Token for the given user.</summary>
    TokenPair GenerateTokenPair(Guid userId, string phoneNumber);

    /// <summary>
    /// Validates a refresh token and returns the embedded <paramref name="userId"/> on success.
    /// Returns <c>null</c> if the token is expired or tampered.
    /// </summary>
    Guid? ValidateRefreshToken(string refreshToken);
}
