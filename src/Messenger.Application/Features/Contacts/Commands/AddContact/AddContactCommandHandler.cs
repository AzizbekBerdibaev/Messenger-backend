using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Contacts.Commands.AddContact;

public sealed class AddContactCommandHandler : IRequestHandler<AddContactCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public AddContactCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AddContactCommand request, CancellationToken cancellationToken)
    {
        // Find the user to add by phone number
        var targetUser = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (targetUser is null)
        {
            throw new KeyNotFoundException($"User with phone number {request.PhoneNumber} not found.");
        }

        if (targetUser.Id == request.UserId)
        {
            throw new InvalidOperationException("You cannot add yourself as a contact.");
        }

        var existingContact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.ContactUserId == targetUser.Id, cancellationToken);

        if (existingContact is not null)
        {
            // Already a contact, maybe we could update the name, but for now just return the ID.
            return targetUser.Id;
        }

        var contact = Contact.Create(request.UserId, targetUser.Id, request.FirstName, request.LastName);

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);

        return targetUser.Id;
    }
}
