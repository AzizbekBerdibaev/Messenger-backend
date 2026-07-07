using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Messenger.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Messenger.Infrastructure.Services;

/// <summary>
/// Generates and validates JWT access + refresh tokens using HMAC-SHA256 signing.
/// </summary>
public sealed class JwtService : IJwtService
{
    private readonly JwtSettings           _settings;
    private readonly JwtSecurityTokenHandler _handler;
    private readonly SymmetricSecurityKey   _signingKey;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings   = settings.Value;
        _handler    = new JwtSecurityTokenHandler();
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    }

    /// <inheritdoc/>
    public TokenPair GenerateTokenPair(Guid userId, string phoneNumber)
    {
        var now              = DateTime.UtcNow;
        var accessExpiry     = now.AddMinutes(_settings.AccessTokenExpirationMinutes);
        var refreshExpiry    = now.AddDays(_settings.RefreshTokenExpirationDays);
        var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        // ── Access Token ────────────────────────────────────────────────────────
        var accessClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("phone",                     phoneNumber),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type",                "access")
        };

        var accessDescriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(accessClaims),
            Issuer             = _settings.Issuer,
            Audience           = _settings.Audience,
            Expires            = accessExpiry,
            NotBefore          = now,
            SigningCredentials = signingCredentials
        };

        var accessToken = _handler.WriteToken(_handler.CreateToken(accessDescriptor));

        // ── Refresh Token ───────────────────────────────────────────────────────
        var refreshClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_type",                "refresh")
        };

        var refreshDescriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(refreshClaims),
            Issuer             = _settings.Issuer,
            Audience           = _settings.Audience,
            Expires            = refreshExpiry,
            NotBefore          = now,
            SigningCredentials = signingCredentials
        };

        var refreshToken = _handler.WriteToken(_handler.CreateToken(refreshDescriptor));

        return new TokenPair(accessToken, refreshToken, accessExpiry);
    }

    /// <inheritdoc/>
    public Guid? ValidateRefreshToken(string refreshToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = _signingKey,
            ValidateIssuer           = true,
            ValidIssuer              = _settings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = _settings.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };

        try
        {
            var principal = _handler.ValidateToken(refreshToken, validationParameters, out _);

            // Ensure this is a refresh token, not an access token being replayed
            var tokenTypeClaim = principal.FindFirstValue("token_type");
            if (tokenTypeClaim != "refresh")
            {
                return null;
            }

            var subClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(subClaim, out var userId) ? userId : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
