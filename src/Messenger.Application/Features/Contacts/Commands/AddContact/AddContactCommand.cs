using MediatR;

namespace Messenger.Application.Features.Contacts.Commands.AddContact;

/// <summary>
/// Command: Adds a user to the current user's address book.
/// </summary>
public sealed record AddContactCommand(
    Guid    UserId,
    string  PhoneNumber,
    string  FirstName,
    string? LastName) : IRequest<Guid>;
