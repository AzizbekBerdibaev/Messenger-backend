using MediatR;
using Messenger.Application.Common.Interfaces;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Auth.Commands.VerifyOtp;

/// <summary>
/// Handles <see cref="VerifyOtpCommand"/>.
/// Steps:
/// 1. Validates the OTP against Redis cache via <see cref="IOtpService"/>.
/// 2. Upserts the <see cref="User"/> record in PostgreSQL.
/// 3. Generates and returns a JWT token pair.
/// </summary>
public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
{
    private readonly IOtpService _otpService;
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        IOtpService otpService,
        IJwtService jwtService,
        IApplicationDbContext db,
        ILogger<VerifyOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _jwtService = jwtService;
        _db = db;
        _logger = logger;
    }

    public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Validate OTP ──────────────────────────────────────────────
        var isValid = await _otpService.ValidateAsync(request.PhoneNumber, request.Code, cancellationToken);
        if (!isValid)
        {
            throw new UnauthorizedAccessException(
                $"Invalid or expired OTP for phone number {request.PhoneNumber}.");
        }

        // ── Step 2: Upsert User ───────────────────────────────────────────────
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        bool isNewUser;
        User user;

        if (existingUser is null)
        {
            user = User.Create(Guid.NewGuid(), request.PhoneNumber);
            isNewUser = true;
            _db.Users.Add(user);

            _logger.LogInformation("Creating new user for phone {PhoneNumber}", request.PhoneNumber);
        }
        else
        {
            user = existingUser;
            isNewUser = false;
            user.TouchLastSeen();

            _logger.LogInformation("Existing user {UserId} authenticated", user.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // ── Step 3: Issue JWT ─────────────────────────────────────────────────
        var tokens = _jwtService.GenerateTokenPair(user.Id, user.PhoneNumber);

        return new VerifyOtpResult(
            UserId: user.Id,
            PhoneNumber: user.PhoneNumber,
            IsNewUser: isNewUser,
            Tokens: tokens);
    }
}
