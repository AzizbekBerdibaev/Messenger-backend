using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Chats.Commands.CreateGroupChat;
using Messenger.Application.Features.Chats.Commands.CreatePrivateChat;
using Messenger.Application.Features.Chats.Queries.GetChatMessages;
using Messenger.Application.Features.Chats.Queries.GetChats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

/// <summary>
/// Manages chat resources: list, create private, create group, and retrieve messages.
/// All endpoints require a valid JWT (Bearer scheme).
/// </summary>
[ApiController]
[Route("api/chats")]
[Authorize]
[Produces("application/json")]
public sealed class ChatsController : ControllerBase
{
    private readonly ISender _sender;

    public ChatsController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all chats the current user is a member of.</summary>
    /// <response code="200">List of chats (newest activity first).</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChatSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetChats(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(new GetChatsQuery(userId), ct);
        return Ok(result);
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a private (1-to-1) chat with another user. Idempotent.</summary>
    /// <response code="200">Chat created or already existed.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="404">Other user not found.</response>
    [HttpPost("private")]
    [ProducesResponseType(typeof(CreatePrivateChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePrivateChat(
        [FromBody] CreatePrivateChatRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(
            new CreatePrivateChatCommand(userId, request.OtherUserId), ct);

        return Ok(new CreatePrivateChatResponse(
            result.ChatId,
            result.AlreadyExisted,
            result.CreatedAt));
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a group chat with the current user as creator.</summary>
    /// <response code="201">Group chat created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="404">One or more users not found.</response>
    [HttpPost("group")]
    [ProducesResponseType(typeof(CreateGroupChatResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGroupChat(
        [FromBody] CreateGroupChatRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(
            new CreateGroupChatCommand(userId, request.ParticipantIds), ct);

        return CreatedAtAction(
            actionName:    nameof(GetChatMessages),
            routeValues:   new { chatId = result.ChatId },
            value: new CreateGroupChatResponse(
                result.ChatId,
                result.CreatedAt,
                result.ParticipantCount));
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns paginated message history for a chat.</summary>
    /// <param name="chatId">Chat identifier.</param>
    /// <param name="before">Cursor — return messages older than this UTC timestamp.</param>
    /// <param name="limit">Page size (1–100, default 50).</param>
    /// <response code="200">Page of messages with optional next-cursor.</response>
    /// <response code="401">Not authenticated or not a chat member.</response>
    [HttpGet("{chatId:guid}/messages", Name = nameof(GetChatMessages))]
    [ProducesResponseType(typeof(GetChatMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetChatMessages(
        Guid              chatId,
        [FromQuery] DateTime? before,
        [FromQuery] int       limit = 50,
        CancellationToken     ct    = default)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(
            new GetChatMessagesQuery(chatId, userId, before, limit), ct);

        return Ok(new GetChatMessagesResponse(result.Messages, result.NextCursor));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User identity claim is missing.");

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("User identity claim is not a valid GUID.");
    }
}

// ── Request / Response DTOs ──────────────────────────────────────────────────

public sealed record CreatePrivateChatRequest(Guid OtherUserId);

public sealed record CreatePrivateChatResponse(
    Guid     ChatId,
    bool     AlreadyExisted,
    DateTime CreatedAt);

public sealed record CreateGroupChatRequest(List<Guid> ParticipantIds);

public sealed record CreateGroupChatResponse(
    Guid     ChatId,
    DateTime CreatedAt,
    int      ParticipantCount);

public sealed record GetChatMessagesResponse(
    List<MessageDto> Messages,
    DateTime?        NextCursor);
