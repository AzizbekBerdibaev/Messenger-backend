namespace Messenger.Domain.Entities;

/// <summary>
/// Represents a privacy setting where a user blocks another user.
/// </summary>
public class BlockedUser
{
    /// <summary>The user who initiated the block.</summary>
    public Guid UserId { get; private set; }
    
    /// <summary>The user who is blocked.</summary>
    public Guid BlockedUserId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public User? BlockedUserNavigation { get; private set; }

    private BlockedUser() { } // For EF Core

    private BlockedUser(Guid userId, Guid blockedUserId)
    {
        UserId        = userId;
        BlockedUserId = blockedUserId;
        CreatedAt     = DateTime.UtcNow;
    }

    public static BlockedUser Create(Guid userId, Guid blockedUserId)
        => new(userId, blockedUserId);
}
