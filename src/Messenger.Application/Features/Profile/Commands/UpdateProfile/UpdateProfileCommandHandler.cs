using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Profile.Commands.UpdateProfile;

/// <summary>
/// Handles <see cref="UpdateProfileCommand"/>.
/// Loads the user, calls the domain method <c>UpdateProfile</c>, persists and returns the result.
/// </summary>
public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    private readonly IApplicationDbContext _db;

    public UpdateProfileCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateProfileResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        user.UpdateProfile(request.FirstName, request.LastName, request.AvatarUrl);

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateProfileResult(
            UserId: user.Id,
            FirstName: user.FirstName,
            LastName: user.LastName,
            AvatarUrl: user.AvatarUrl);
    }
}
