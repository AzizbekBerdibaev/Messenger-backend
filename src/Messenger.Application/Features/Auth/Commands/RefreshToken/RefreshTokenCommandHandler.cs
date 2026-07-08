using MediatR;
using Messenger.Application.Common.Interfaces;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handles <see cref="RefreshTokenCommand"/>.
/// Validates the incoming refresh token, looks up the user, and issues a fresh token pair.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _db;

    public RefreshTokenCommandHandler(IJwtService jwtService, IApplicationDbContext db)
    {
        _jwtService = jwtService;
        _db = db;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtService.ValidateRefreshToken(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Refresh token is invalid or has expired.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        user.TouchLastSeen();
        await _db.SaveChangesAsync(cancellationToken);

        var tokens = _jwtService.GenerateTokenPair(user.Id, user.PhoneNumber);

        return new RefreshTokenResult(
            AccessToken: tokens.AccessToken,
            RefreshToken: tokens.RefreshToken,
            AccessTokenExpiresAt: tokens.AccessTokenExpiresAt);
    }
}
