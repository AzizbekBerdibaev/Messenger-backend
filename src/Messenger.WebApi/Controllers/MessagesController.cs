using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Messages.Commands.MarkMessagesRead;
using Messenger.Application.Features.Messages.Commands.SendMessage;
using Messenger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

/// <summary>
/// Manages messages within a chat: send a message and mark messages as read.
/// All endpoints require a valid JWT (Bearer scheme).
/// </summary>
[ApiController]
[Route("api/chats/{chatId:guid}/messages")]
[Authorize]
[Produces("application/json")]
public sealed class MessagesController : ControllerBase
{
    private readonly ISender _sender;

    public MessagesController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Sends a message to the specified chat.</summary>
    /// <response code="201">Message created and delivered to recipients via SignalR.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not authenticated or not a chat member.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendMessage(
        Guid                   chatId,
        [FromBody] SendMessageRequest request,
        CancellationToken      ct)
    {
        var senderId = GetCurrentUserId();
        var result   = await _sender.Send(
            new SendMessageCommand(
                chatId,
                senderId,
                request.Type,
                request.Content,
                request.MediaUrl,
                request.Duration,
                request.ReplyToMessageId),
            ct);

        return StatusCode(StatusCodes.Status201Created, new SendMessageResponse(
            MessageId:        result.MessageId,
            ChatId:           result.ChatId,
            SenderId:         result.SenderId,
            Type:             result.Type,
            Content:          result.Content,
            MediaUrl:         result.MediaUrl,
            Duration:         result.Duration,
            ReplyToMessageId: result.ReplyToMessageId,
            CreatedAt:        result.CreatedAt));
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Marks all messages up to and including the specified message as read.</summary>
    /// <response code="204">Read receipts updated.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Message not found in the chat.</response>
    [HttpPut("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        Guid                       chatId,
        [FromBody] MarkReadRequest request,
        CancellationToken          ct)
    {
        var userId = GetCurrentUserId();
        await _sender.Send(
            new MarkMessagesReadCommand(chatId, userId, request.UpToMessageId),
            ct);

        return NoContent();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Searches messages within the chat.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages(
        Guid chatId,
        [FromQuery] string query,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _sender.Send(
            new Messenger.Application.Features.Search.Queries.SearchMessages.SearchMessagesQuery(chatId, userId, query),
            ct);

        return Ok(result);
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

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record SendMessageRequest(
    MessageType Type,
    string?     Content,
    string?     MediaUrl,
    int?        Duration,
    Guid?       ReplyToMessageId);

public sealed record SendMessageResponse(
    Guid        MessageId,
    Guid        ChatId,
    Guid        SenderId,
    MessageType Type,
    string?     Content,
    string?     MediaUrl,
    int?        Duration,
    Guid?       ReplyToMessageId,
    DateTime    CreatedAt);

public sealed record MarkReadRequest(Guid UpToMessageId);
