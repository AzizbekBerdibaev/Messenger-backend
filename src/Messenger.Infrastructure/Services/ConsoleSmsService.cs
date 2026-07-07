using Messenger.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Services;

/// <summary>
/// Mock SMS service. In development it logs the message to the console/Serilog.
/// In production, swap this registration for a real provider (Twilio, AWS SNS, etc.)
/// without changing any application code.
/// </summary>
public sealed class ConsoleSmsService : ISmsService
{
    private readonly ILogger<ConsoleSmsService> _logger;

    public ConsoleSmsService(ILogger<ConsoleSmsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        // In a real implementation this would call an SMS gateway.
        _logger.LogInformation(
            "[SMS MOCK] To: {PhoneNumber} | Message: {Message}",
            phoneNumber,
            message);

        return Task.CompletedTask;
    }
}
