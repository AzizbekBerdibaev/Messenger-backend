using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Contacts.Queries.GetContacts;

public sealed class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, List<ContactDto>>
{
    private readonly IApplicationDbContext _db;

    public GetContactsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ContactDto>> Handle(GetContactsQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _db.Contacts
            .Where(c => c.UserId == request.UserId)
            .Select(c => new
            {
                c.ContactUserId,
                // Contact entity holds the name preferred by the user. If they want to sync it, it's stored here.
                c.FirstName,
                c.LastName,
                AvatarUrl = c.ContactUser!.AvatarUrl,
                LastSeenAt = c.ContactUser.LastSeenAt
            })
            .OrderBy(c => c.FirstName)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        return contacts.Select(c => new ContactDto(
            ContactUserId: c.ContactUserId,
            FirstName:     c.FirstName,
            LastName:      c.LastName,
            AvatarUrl:     c.AvatarUrl,
            IsOnline:      (now - c.LastSeenAt).TotalMinutes < 5,
            LastSeenAt:    c.LastSeenAt
        )).ToList();
    }
}
