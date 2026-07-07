namespace Messenger.Application.Common.Interfaces;

/// <summary>
/// Mock SMS gateway — in production, replace with a real SMS provider (Twilio, etc.).
/// Marked as a separate interface so it can be trivially swapped without touching business logic.
/// </summary>
public interface ISmsService
{
    /// <summary>Sends <paramref name="message"/> to <paramref name="phoneNumber"/>.</summary>
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
