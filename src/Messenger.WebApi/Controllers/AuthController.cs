using MediatR;
using Messenger.Application.Features.Auth.Commands.RefreshToken;
using Messenger.Application.Features.Auth.Commands.SendOtp;
using Messenger.Application.Features.Auth.Commands.VerifyOtp;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

/// <summary>
/// Handles authentication via OTP: send code, verify code, refresh JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Sends a 4-digit OTP to the supplied phone number.</summary>
    /// <response code="204">OTP sent successfully.</response>
    /// <response code="400">Validation error (invalid phone format).</response>
    [HttpPost("send-otp")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken ct)
    {
        await _sender.Send(new SendOtpCommand(request.PhoneNumber), ct);
        return NoContent();
    }

    /// <summary>Verifies the OTP and returns JWT tokens.</summary>
    /// <response code="200">OTP verified; JWT pair returned.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Invalid or expired OTP.</response>
    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new VerifyOtpCommand(request.PhoneNumber, request.Code), ct);

        return Ok(new VerifyOtpResponse(
            UserId:               result.UserId,
            PhoneNumber:          result.PhoneNumber,
            IsNewUser:            result.IsNewUser,
            AccessToken:          result.Tokens.AccessToken,
            RefreshToken:         result.Tokens.RefreshToken,
            AccessTokenExpiresAt: result.Tokens.AccessTokenExpiresAt));
    }

    /// <summary>Rotates the JWT pair using a valid refresh token.</summary>
    /// <response code="200">New token pair returned.</response>
    /// <response code="401">Refresh token is invalid or expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);

        return Ok(new RefreshTokenResponse(
            AccessToken:          result.AccessToken,
            RefreshToken:         result.RefreshToken,
            AccessTokenExpiresAt: result.AccessTokenExpiresAt));
    }
}

// ── Request / Response DTOs (thin transport models, not domain objects) ──────

public sealed record SendOtpRequest(string PhoneNumber);

public sealed record VerifyOtpRequest(string PhoneNumber, string Code);

public sealed record VerifyOtpResponse(
    Guid     UserId,
    string   PhoneNumber,
    bool     IsNewUser,
    string   AccessToken,
    string   RefreshToken,
    DateTime AccessTokenExpiresAt);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RefreshTokenResponse(
    string   AccessToken,
    string   RefreshToken,
    DateTime AccessTokenExpiresAt);
