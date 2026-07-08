namespace Messenger.Domain.Enums;

/// <summary>Delivery / read status of a message for a specific recipient.</summary>
public enum DeliveryStatus
{
    Sent = 1,
    Delivered = 2,
    Read = 3
}
