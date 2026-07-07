using MediatR;
using Messenger.Application.Common.Interfaces;

namespace Messenger.Application.Features.Auth.Commands.VerifyOtp;

/// <summary>
/// Command: Verifies the OTP entered by the user.
/// On success, creates or retrieves the <c>User</c> record and returns a JWT token pair.
/// </summary>
public sealed record VerifyOtpCommand(string PhoneNumber, string Code) : IRequest<VerifyOtpResult>;

/// <summary>Result returned on successful OTP verification.</summary>
public sealed record VerifyOtpResult(
    Guid        UserId,
    string      PhoneNumber,
    bool        IsNewUser,
    TokenPair   Tokens);
