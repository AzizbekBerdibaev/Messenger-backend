namespace Messenger.Application.Common.Interfaces;

/// <summary>
/// Abstraction for one-time password generation and verification.
/// The actual implementation stores codes in Redis with a TTL.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a 4-digit OTP, caches it in Redis keyed by <paramref name="phoneNumber"/>
    /// for <paramref name="ttl"/> and returns the code.
    /// </summary>
    Task<string> GenerateAndStoreAsync(string phoneNumber, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Validates the supplied <paramref name="code"/> against the value cached in Redis.
    /// Returns <c>true</c> and invalidates the entry on success; <c>false</c> otherwise.
    /// </summary>
    Task<bool> ValidateAsync(string phoneNumber, string code, CancellationToken ct = default);
}
