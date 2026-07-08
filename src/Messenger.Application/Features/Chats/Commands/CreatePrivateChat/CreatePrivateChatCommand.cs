using MediatR;

namespace Messenger.Application.Features.Chats.Commands.CreatePrivateChat;

/// <summary>
/// Command: Creates a private (1-to-1) chat between the current user and another user.
/// Idempotent — returns the existing chat if one already exists between the two users.
/// </summary>
public sealed record CreatePrivateChatCommand(
    Guid CurrentUserId,
    Guid OtherUserId) : IRequest<CreatePrivateChatResult>;

/// <summary>Result returned after creating (or retrieving existing) a private chat.</summary>
public sealed record CreatePrivateChatResult(
    Guid ChatId,
    bool AlreadyExisted,
    DateTime CreatedAt);
