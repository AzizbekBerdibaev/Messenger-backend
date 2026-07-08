using MediatR;

namespace Messenger.Application.Features.Contacts.Queries.GetContacts;

public sealed record GetContactsQuery(Guid UserId) : IRequest<List<ContactDto>>;

public sealed record ContactDto(
    Guid ContactUserId,
    string FirstName,
    string? LastName,
    string? AvatarUrl,
    bool IsOnline,
    DateTime LastSeenAt);
