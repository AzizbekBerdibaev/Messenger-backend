namespace Messenger.Domain.Entities;

/// <summary>
/// A 24-hour story published by a <see cref="User"/>.
/// Stories expire at <see cref="ExpiresAt"/> and are cleaned up by a background service.
/// </summary>
public sealed class Story
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MediaUrl { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Navigation
    public User? User { get; private set; }

    private Story() { }

    public static Story Create(Guid userId, string mediaUrl, TimeSpan? lifetime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaUrl);
        var now = DateTime.UtcNow;
        return new Story
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MediaUrl = mediaUrl,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime ?? TimeSpan.FromHours(24))
        };
    }

    /// <summary>Returns <c>true</c> when the story has not yet expired.</summary>
    public bool IsActive(DateTime utcNow) => ExpiresAt > utcNow;
}
