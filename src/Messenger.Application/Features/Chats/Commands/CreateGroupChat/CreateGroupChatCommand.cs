using MediatR;

namespace Messenger.Application.Features.Chats.Commands.CreateGroupChat;

/// <summary>
/// Command: Creates a group chat with a specified set of participants.
/// The <see cref="CreatorId"/> is always added as a participant.
/// </summary>
public sealed record CreateGroupChatCommand(
    Guid CreatorId,
    List<Guid> ParticipantIds) : IRequest<CreateGroupChatResult>;

/// <summary>Result returned after creating a group chat.</summary>
public sealed record CreateGroupChatResult(
    Guid ChatId,
    DateTime CreatedAt,
    int ParticipantCount);
