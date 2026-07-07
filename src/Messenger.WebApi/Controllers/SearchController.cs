using MediatR;
using Messenger.Application.Features.Search.Queries.SearchUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        var result = await _mediator.Send(new SearchUsersQuery(query));
        return Ok(result);
    }
}
