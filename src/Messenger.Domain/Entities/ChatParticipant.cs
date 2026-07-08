namespace Messenger.Domain.Entities;

/// <summary>
/// Join entity representing membership of a <see cref="User"/> in a <see cref="Chat"/>.
/// Uses a composite primary key {ChatId, UserId}.
/// </summary>
public sealed class ChatParticipant
{
    public Guid ChatId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Navigation properties
    public Chat? Chat { get; private set; }
    public User? User { get; private set; }

    private ChatParticipant() { }

    public static ChatParticipant Create(Guid chatId, Guid userId)
    {
        return new ChatParticipant
        {
            ChatId = chatId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
    }
}
