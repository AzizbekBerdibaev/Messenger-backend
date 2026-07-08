using Messenger.Domain.Enums;

namespace Messenger.Domain.Entities;

/// <summary>
/// Represents a user's device registered for push notifications.
/// </summary>
public class UserDevice
{
    public Guid UserId { get; private set; }
    public string DeviceToken { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public User? User { get; private set; }

    private UserDevice()
    {
        DeviceToken = string.Empty;
    }

    private UserDevice(Guid userId, string deviceToken, DeviceType deviceType)
    {
        UserId = userId;
        DeviceToken = deviceToken;
        DeviceType = deviceType;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static UserDevice Create(Guid userId, string deviceToken, DeviceType deviceType)
        => new(userId, deviceToken, deviceType);

    public void UpdateToken(string newDeviceToken)
    {
        DeviceToken = newDeviceToken;
        UpdatedAt = DateTime.UtcNow;
    }
}
