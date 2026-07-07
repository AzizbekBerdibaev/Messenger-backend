using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDto>
{
    private readonly IApplicationDbContext _db;

    public GetProfileQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new ProfileDto(
                u.Id,
                u.PhoneNumber,
                u.FirstName,
                u.LastName,
                u.AvatarUrl))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException($"User {request.UserId} not found.");
        }

        return user;
    }
}
