using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Privacy.Commands.BlockUser;
using Messenger.Application.Features.Privacy.Commands.UnblockUser;
using Messenger.Application.Features.Privacy.Queries.GetBlockedUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrivacyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new BlockUserCommand(userId, request.UserIdToBlock));
        return Ok();
    }

    [HttpPost("unblock")]
    public async Task<IActionResult> UnblockUser([FromBody] UnblockUserRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new UnblockUserCommand(userId, request.UserIdToUnblock));
        return Ok();
    }

    [HttpGet("blocked-users")]
    public async Task<IActionResult> GetBlockedUsers()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _mediator.Send(new GetBlockedUsersQuery(userId));
        return Ok(result);
    }
}

public record BlockUserRequest(Guid UserIdToBlock);
public record UnblockUserRequest(Guid UserIdToUnblock);
