namespace Messenger.Infrastructure.Services;

/// <summary>
/// Strongly-typed configuration for JWT generation.
/// Bound from <c>appsettings.json</c> section <c>JwtSettings</c>.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>Symmetric signing secret — at least 256 bits (32 chars).</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>Token issuer (e.g. "messenger-api").</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Token audience (e.g. "messenger-mobile").</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
