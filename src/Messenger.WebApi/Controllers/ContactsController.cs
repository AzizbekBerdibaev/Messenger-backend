using System.Security.Claims;
using MediatR;
using Messenger.Application.Features.Contacts.Commands.AddContact;
using Messenger.Application.Features.Contacts.Queries.GetContacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Controllers;

[ApiController]
[Route("api/contacts")]
[Authorize]
[Produces("application/json")]
public sealed class ContactsController : ControllerBase
{
    private readonly ISender _sender;

    public ContactsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Adds a user to the current user's contact list.</summary>
    /// <response code="200">Contact added successfully. Returns the contact User ID.</response>
    /// <response code="400">Validation error or invalid operation (e.g., adding oneself).</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddContact(
        [FromBody] AddContactRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var contactId = await _sender.Send(
            new AddContactCommand(
                UserId:      userId,
                PhoneNumber: request.PhoneNumber,
                FirstName:   request.FirstName,
                LastName:    request.LastName),
            ct);

        return Ok(contactId);
    }

    /// <summary>Returns the current user's contact list.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContacts(CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var contacts = await _sender.Send(new GetContactsQuery(userId), ct);

        return Ok(contacts);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identity claim is missing.");

        return Guid.TryParse(subClaim, out var id)
            ? id
            : throw new UnauthorizedAccessException("User identity claim is not a valid GUID.");
    }
}

public sealed record AddContactRequest(
    string  PhoneNumber,
    string  FirstName,
    string? LastName);
