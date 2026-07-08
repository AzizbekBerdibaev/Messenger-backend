using MediatR;
using Messenger.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Auth.Commands.SendOtp;

/// <summary>
/// Handles <see cref="SendOtpCommand"/>.
/// Generates a 4-digit OTP, caches it in Redis (5 min TTL) and sends it via SMS.
/// </summary>
public sealed class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Unit>
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

    private readonly IOtpService _otpService;
    private readonly ISmsService _smsService;
    private readonly ILogger<SendOtpCommandHandler> _logger;

    public SendOtpCommandHandler(
        IOtpService otpService,
        ISmsService smsService,
        ILogger<SendOtpCommandHandler> logger)
    {
        _otpService = otpService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<Unit> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var code = await _otpService.GenerateAndStoreAsync(request.PhoneNumber, OtpTtl, cancellationToken);

        await _smsService.SendAsync(
            request.PhoneNumber,
            $"Your Messenger OTP is: {code}. It expires in 5 minutes.",
            cancellationToken);

        _logger.LogInformation(
            "OTP dispatched to {PhoneNumber} (expires in {Ttl})",
            request.PhoneNumber,
            OtpTtl);

        return Unit.Value;
    }
}
