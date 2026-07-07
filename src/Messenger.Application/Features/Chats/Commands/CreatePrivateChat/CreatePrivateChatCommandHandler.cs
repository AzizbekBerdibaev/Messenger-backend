using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Application.Features.Chats.Commands.CreatePrivateChat;

/// <summary>
/// Handles <see cref="CreatePrivateChatCommand"/>.
/// Steps:
/// 1. Check if a private chat between the two users already exists.
/// 2. If yes — return it (idempotent).
/// 3. If no — create Chat + two ChatParticipants, persist.
/// </summary>
public sealed class CreatePrivateChatCommandHandler
    : IRequestHandler<CreatePrivateChatCommand, CreatePrivateChatResult>
{
    private readonly IApplicationDbContext              _db;
    private readonly ILogger<CreatePrivateChatCommandHandler> _logger;

    public CreatePrivateChatCommandHandler(
        IApplicationDbContext                   db,
        ILogger<CreatePrivateChatCommandHandler> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<CreatePrivateChatResult> Handle(
        CreatePrivateChatCommand command,
        CancellationToken        cancellationToken)
    {
        // ── Step 1: Check for existing private chat ───────────────────────────
        var existingChat = await _db.Chats
            .Where(c => c.Type == ChatType.Private)
            .Where(c => c.Participants.Any(p => p.UserId == command.CurrentUserId))
            .Where(c => c.Participants.Any(p => p.UserId == command.OtherUserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingChat is not null)
        {
            _logger.LogInformation(
                "Private chat {ChatId} already exists between {UserA} and {UserB}",
                existingChat.Id, command.CurrentUserId, command.OtherUserId);

            return new CreatePrivateChatResult(
                ChatId:        existingChat.Id,
                AlreadyExisted: true,
                CreatedAt:     existingChat.CreatedAt);
        }

        // ── Step 2: Verify both users exist ───────────────────────────────────
        var userExists = await _db.Users.AnyAsync(u => u.Id == command.OtherUserId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User {command.OtherUserId} not found.");
        }

        // ── Step 3: Create chat + participants ────────────────────────────────
        var chat = Chat.CreatePrivate();
        _db.Chats.Add(chat);

        _db.ChatParticipants.Add(ChatParticipant.Create(chat.Id, command.CurrentUserId));
        _db.ChatParticipants.Add(ChatParticipant.Create(chat.Id, command.OtherUserId));

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created private chat {ChatId} between {UserA} and {UserB}",
            chat.Id, command.CurrentUserId, command.OtherUserId);

        return new CreatePrivateChatResult(
            ChatId:        chat.Id,
            AlreadyExisted: false,
            CreatedAt:     chat.CreatedAt);
    }
}
