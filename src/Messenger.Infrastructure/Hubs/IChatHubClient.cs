using Messenger.Application.Features.Chats.Queries.GetChatMessages;
using Messenger.Domain.Enums;

namespace Messenger.Infrastructure.Hubs;

/// <summary>
/// Typed client interface for the SignalR <see cref="ChatHub"/>.
/// Methods defined here are called on the client side.
/// </summary>
public interface IChatHubClient
{
    /// <summary>Pushes a new message to all members of the chat group.</summary>
    Task ReceiveMessage(ChatHubMessageDto message);

    /// <summary>Notifies that messages were read up to a given point.</summary>
    Task MessagesRead(ChatHubReadReceiptDto receipt);

    /// <summary>Notifies that a user is typing.</summary>
    Task UserTyping(Guid chatId, Guid userId);

    /// <summary>WebRTC: Call Offer</summary>
    Task ReceiveCallOffer(Guid chatId, Guid callerId, string sdp);

    /// <summary>WebRTC: Call Answer</summary>
    Task ReceiveCallAnswer(Guid chatId, Guid answererId, string sdp);

    /// <summary>WebRTC: ICE Candidate</summary>
    Task ReceiveIceCandidate(Guid chatId, Guid senderId, string candidate);
}

/// <summary>Payload sent to clients when a new message arrives.</summary>
public sealed record ChatHubMessageDto(
    Guid MessageId,
    Guid ChatId,
    Guid SenderId,
    string SenderFirstName,
    string? SenderAvatarUrl,
    MessageType Type,
    string? Content,
    string? MediaUrl,
    Guid? ReplyToMessageId,
    DateTime CreatedAt);

/// <summary>Payload sent to clients when messages are marked as read.</summary>
public sealed record ChatHubReadReceiptDto(
    Guid ChatId,
    Guid ReaderId,
    Guid UpToMessageId);
