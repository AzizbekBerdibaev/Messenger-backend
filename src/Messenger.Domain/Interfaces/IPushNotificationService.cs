namespace Messenger.Domain.Interfaces;

/// <summary>
/// Abstraction for pushing notifications to mobile/web clients (e.g. Firebase Cloud Messaging).
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends a push notification to a specific device.
    /// </summary>
    /// <param name="deviceToken">The FCM/APNs token.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body/message.</param>
    /// <param name="data">Additional payload data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNotificationAsync(string deviceToken, string title, string body, IDictionary<string, string>? data = null);
}
