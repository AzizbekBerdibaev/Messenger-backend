using Messenger.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Services;

public sealed class DummyPushNotificationService : IPushNotificationService
{
    private readonly ILogger<DummyPushNotificationService> _logger;

    public DummyPushNotificationService(ILogger<DummyPushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendNotificationAsync(string deviceToken, string title, string body, IDictionary<string, string>? data = null)
    {
        _logger.LogInformation("Sending Push to {DeviceToken}: {Title} - {Body}", deviceToken, title, body);
        return Task.CompletedTask;
    }
}
