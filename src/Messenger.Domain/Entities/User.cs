namespace Messenger.Domain.Entities;

/// <summary>
/// Represents an application user. The primary authentication identifier is
/// <see cref="PhoneNumber"/>; passwords are never stored — OTP-only flow.
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime LastSeenAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties (EF Core)
    public ICollection<ChatParticipant> ChatParticipants { get; private set; } = [];
    public ICollection<Message> SentMessages { get; private set; } = [];
    public ICollection<MessageStatus> MessageStatuses { get; private set; } = [];
    public ICollection<Story> Stories { get; private set; } = [];

    /// <summary>EF Core constructor — do not use directly in application code.</summary>
    private User() { }

    /// <summary>Creates a new user record at the moment of first OTP verification.</summary>
    public static User Create(Guid id, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        return new User
        {
            Id = id,
            PhoneNumber = phoneNumber,
            FirstName = string.Empty,
            LastSeenAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Fills in profile data after the first OTP verification or via profile update.</summary>
    public void UpdateProfile(string firstName, string? lastName, string? avatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
    }

    /// <summary>Refreshes the last-seen timestamp (called on every authenticated request).</summary>
    public void TouchLastSeen() => LastSeenAt = DateTime.UtcNow;
}
