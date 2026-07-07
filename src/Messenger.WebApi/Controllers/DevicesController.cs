using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Devices.Commands.RegisterDeviceToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new RegisterDeviceTokenCommand(userId, request.DeviceToken, request.DeviceType);
        await _mediator.Send(command);
        return Ok();
    }
}

public record RegisterDeviceRequest(string DeviceToken, Messenger.Domain.Enums.DeviceType DeviceType);
