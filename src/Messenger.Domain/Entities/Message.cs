using Messenger.Domain.Enums;

namespace Messenger.Domain.Entities;

/// <summary>
/// A single message inside a <see cref="Chat"/>.
/// Supports text, image and audio types.
/// Nullable <see cref="ReplyToMessageId"/> enables threaded replies.
/// </summary>
public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid ChatId { get; private set; }
    public Guid SenderId { get; private set; }
    public MessageType Type { get; private set; }
    public string? Content { get; private set; }
    public string? MediaUrl { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }
    public int? Duration { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Chat? Chat { get; private set; }
    public User? Sender { get; private set; }
    public Message? ReplyToMessage { get; private set; }
    public ICollection<MessageStatus> Statuses { get; private set; } = [];

    private Message() { }

    /// <summary>Creates a text message.</summary>
    public static Message CreateText(Guid chatId, Guid senderId, string content, Guid? replyToMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            Type = MessageType.Text,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Creates an image message with a pre-uploaded S3 URL and optional text caption.</summary>
    public static Message CreateImage(Guid chatId, Guid senderId, string mediaUrl, string? content = null, Guid? replyToMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaUrl);
        return new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            Type = MessageType.Image,
            Content = content,
            MediaUrl = mediaUrl,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Creates an audio (voice) message with a pre-uploaded S3 URL.</summary>
    public static Message CreateAudio(Guid chatId, Guid senderId, string mediaUrl, int? duration = null, Guid? replyToMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaUrl);
        return new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderId,
            Type = MessageType.Audio,
            MediaUrl = mediaUrl,
            Duration = duration,
            ReplyToMessageId = replyToMessageId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
