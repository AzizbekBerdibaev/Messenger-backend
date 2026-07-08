using Messenger.Domain.Enums;

namespace Messenger.Domain.Entities;

/// <summary>
/// Represents a conversation (either private 1-to-1 or a group chat).
/// Participants are managed via <see cref="ChatParticipant"/> join entity.
/// </summary>
public sealed class Chat
{
    public Guid Id { get; private set; }
    public ChatType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public ICollection<ChatParticipant> Participants { get; private set; } = [];
    public ICollection<Message> Messages { get; private set; } = [];

    private Chat() { }

    public static Chat CreatePrivate()
    {
        return new Chat
        {
            Id = Guid.NewGuid(),
            Type = ChatType.Private,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Chat CreateGroup()
    {
        return new Chat
        {
            Id = Guid.NewGuid(),
            Type = ChatType.Group,
            CreatedAt = DateTime.UtcNow
        };
    }
}
