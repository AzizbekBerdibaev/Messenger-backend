using System.Security.Claims;
using Messenger.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messenger.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time chat functionality.
/// Each authenticated user is automatically added to SignalR groups
/// corresponding to their chat memberships on connection.
/// Group name convention: <c>chat:{chatId}</c>
/// </summary>
[Authorize]
public sealed class ChatHub : Hub<IChatHubClient>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IApplicationDbContext db, ILogger<ChatHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects. Adds the connection to all
    /// SignalR groups for the user's chats.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();

        // Load all chat IDs the user participates in
        var chatIds = await _db.ChatParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ChatId)
            .ToListAsync();

        // Join SignalR group for each chat
        var joinTasks = chatIds.Select(chatId =>
            Groups.AddToGroupAsync(Context.ConnectionId, ChatGroupName(chatId)));

        await Task.WhenAll(joinTasks);

        _logger.LogInformation(
            "User {UserId} connected (ConnectionId={ConnectionId}), joined {Count} chat groups",
            userId, Context.ConnectionId, chatIds.Count);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// SignalR automatically removes the connection from all groups.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation(
            "User {UserId} disconnected (ConnectionId={ConnectionId})",
            userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // ── WebRTC & Typing ────────────────────────────────────────────────────────

    public async Task Typing(Guid chatId)
    {
        var userId = GetCurrentUserId();
        // Send to all other connected clients in the chat group
        await Clients.OthersInGroup(ChatGroupName(chatId)).UserTyping(chatId, userId);
    }

    public async Task CallOffer(Guid chatId, Guid targetUserId, string sdp)
    {
        var callerId = GetCurrentUserId();
        // Send directly to the target user
        await Clients.User(targetUserId.ToString()).ReceiveCallOffer(chatId, callerId, sdp);
    }

    public async Task CallAnswer(Guid chatId, Guid targetUserId, string sdp)
    {
        var answererId = GetCurrentUserId();
        await Clients.User(targetUserId.ToString()).ReceiveCallAnswer(chatId, answererId, sdp);
    }

    public async Task IceCandidate(Guid chatId, Guid targetUserId, string candidate)
    {
        var senderId = GetCurrentUserId();
        await Clients.User(targetUserId.ToString()).ReceiveIceCandidate(chatId, senderId, candidate);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the canonical SignalR group name for a chat.</summary>
    public static string ChatGroupName(Guid chatId) => $"chat:{chatId}";

    private Guid GetCurrentUserId()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? Context.User?.FindFirstValue("sub")
               ?? throw new HubException("User identity claim is missing.");

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new HubException("User identity claim is not a valid GUID.");
    }
}
