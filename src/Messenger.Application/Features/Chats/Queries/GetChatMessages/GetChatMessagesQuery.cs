using MediatR;
using Messenger.Domain.Enums;

namespace Messenger.Application.Features.Chats.Queries.GetChatMessages;

/// <summary>
/// Query: Returns paginated messages for a chat using cursor-based pagination.
/// The cursor is the <c>CreatedAt</c> timestamp of the oldest message in the previous page.
/// </summary>
public sealed record GetChatMessagesQuery(
    Guid      ChatId,
    Guid      UserId,
    DateTime? BeforeCursor,
    int       Limit = 50) : IRequest<GetChatMessagesResult>;

/// <summary>Paged result containing messages and an optional next-page cursor.</summary>
public sealed record GetChatMessagesResult(
    List<MessageDto> Messages,
    DateTime?        NextCursor);

/// <summary>DTO representing a single message in the chat history.</summary>
public sealed record MessageDto(
    Guid            MessageId,
    Guid            ChatId,
    Guid            SenderId,
    string          SenderFirstName,
    string?         SenderAvatarUrl,
    MessageType     Type,
    string?         Content,
    string?         MediaUrl,
    int?            Duration,
    Guid?           ReplyToMessageId,
    DateTime        CreatedAt,
    DeliveryStatus? MyStatus);
