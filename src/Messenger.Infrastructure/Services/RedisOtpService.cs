using Messenger.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Messenger.Infrastructure.Services;

/// <summary>
/// Generates cryptographically-random 4-digit OTPs, stores them in Redis with a TTL,
/// and validates submitted codes.
/// </summary>
public sealed class RedisOtpService : IOtpService
{
    private const string KeyPrefix = "otp:";

    private readonly IConnectionMultiplexer _redis;

    public RedisOtpService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAndStoreAsync(
        string phoneNumber,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        // 4-digit code with leading zero support ("0042")
        var code = Random.Shared.Next(0, 10_000).ToString("D4");
        var key  = BuildKey(phoneNumber);

        var db = _redis.GetDatabase();
        await db.StringSetAsync(key, code, ttl);

        return code;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default)
    {
        var key = BuildKey(phoneNumber);
        var db  = _redis.GetDatabase();

        // Atomic get-and-delete to prevent replay attacks
        var storedCode = await db.StringGetDeleteAsync(key);
        if (storedCode.IsNullOrEmpty)
        {
            return false;
        }

        return string.Equals(storedCode, code, StringComparison.Ordinal);
    }

    private static string BuildKey(string phoneNumber)
        => $"{KeyPrefix}{phoneNumber}";
}
