using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Chats.Commands.CreateGroupChat;

/// <summary>
/// Handles <see cref="CreateGroupChatCommand"/>.
/// Creates a group chat, adds the creator + all specified participants.
/// </summary>
public sealed class CreateGroupChatCommandHandler
    : IRequestHandler<CreateGroupChatCommand, CreateGroupChatResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<CreateGroupChatCommandHandler> _logger;

    public CreateGroupChatCommandHandler(
        IApplicationDbContext db,
        ILogger<CreateGroupChatCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreateGroupChatResult> Handle(
        CreateGroupChatCommand command,
        CancellationToken cancellationToken)
    {
        // Deduplicate participants and always include the creator
        var allParticipantIds = command.ParticipantIds
            .Append(command.CreatorId)
            .Distinct()
            .ToList();

        // Verify all referenced users exist
        var existingCount = await _db.Users
            .CountAsync(u => allParticipantIds.Contains(u.Id), cancellationToken);

        if (existingCount != allParticipantIds.Count)
        {
            throw new KeyNotFoundException("One or more specified users were not found.");
        }

        // Create the chat
        var chat = Chat.CreateGroup();
        _db.Chats.Add(chat);

        // Add all participants
        foreach (var userId in allParticipantIds)
        {
            _db.ChatParticipants.Add(ChatParticipant.Create(chat.Id, userId));
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created group chat {ChatId} with {Count} participants by user {CreatorId}",
            chat.Id, allParticipantIds.Count, command.CreatorId);

        return new CreateGroupChatResult(
            ChatId: chat.Id,
            CreatedAt: chat.CreatedAt,
            ParticipantCount: allParticipantIds.Count);
    }
}
