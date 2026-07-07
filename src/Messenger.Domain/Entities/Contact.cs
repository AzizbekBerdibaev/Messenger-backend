namespace Messenger.Domain.Entities;

/// <summary>
/// Represents a user's address book contact.
/// </summary>
public sealed class Contact
{
    public Guid   UserId          { get; private set; }
    public Guid   ContactUserId   { get; private set; }
    public string FirstName       { get; private set; } = string.Empty;
    public string? LastName       { get; private set; }
    public DateTime CreatedAt     { get; private set; }

    // Navigation properties
    public User? User             { get; private set; }
    public User? ContactUser      { get; private set; }

    private Contact() { }

    public static Contact Create(Guid userId, Guid contactUserId, string firstName, string? lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);

        return new Contact
        {
            UserId        = userId,
            ContactUserId = contactUserId,
            FirstName     = firstName,
            LastName      = lastName,
            CreatedAt     = DateTime.UtcNow
        };
    }
}
