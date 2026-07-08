using MediatR;
using Messenger.Domain.Enums;

namespace Messenger.Application.Features.Chats.Queries.GetChats;

/// <summary>
/// Query: Returns all chats the current user is a member of,
/// ordered by the most recent message (newest first).
/// </summary>
public sealed record GetChatsQuery(Guid UserId) : IRequest<List<ChatSummaryDto>>;

/// <summary>Summary of a single chat for the chat list view.</summary>
public sealed record ChatSummaryDto(
    Guid ChatId,
    ChatType Type,
    int ParticipantCount,
    DateTime CreatedAt,
    string? ChatName,
    string? ChatAvatarUrl,
    bool IsOnline,
    int UnreadCount,
    LastMessageDto? LastMessage);

/// <summary>Snapshot of the most recent message in a chat.</summary>
public sealed record LastMessageDto(
    Guid MessageId,
    Guid SenderId,
    string SenderFirstName,
    MessageType Type,
    string? Content,
    string? MediaUrl,
    DateTime CreatedAt);
