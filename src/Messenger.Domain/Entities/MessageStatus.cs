using Messenger.Domain.Enums;

namespace Messenger.Domain.Entities;

/// <summary>
/// Tracks the delivery / read status of a <see cref="Message"/> for a specific recipient.
/// Composite PK: {MessageId, UserId}.
/// </summary>
public sealed class MessageStatus
{
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public Message? Message { get; private set; }
    public User? User { get; private set; }

    private MessageStatus() { }

    public static MessageStatus CreateSent(Guid messageId, Guid userId)
    {
        return new MessageStatus
        {
            MessageId = messageId,
            UserId = userId,
            Status = DeliveryStatus.Sent,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Advances the status to <see cref="DeliveryStatus.Delivered"/> if not already further ahead.</summary>
    public void MarkDelivered()
    {
        if (Status < DeliveryStatus.Delivered)
        {
            Status = DeliveryStatus.Delivered;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Advances the status to <see cref="DeliveryStatus.Read"/>.</summary>
    public void MarkRead()
    {
        if (Status < DeliveryStatus.Read)
        {
            Status = DeliveryStatus.Read;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
