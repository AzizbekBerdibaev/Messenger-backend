using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Privacy.Queries.GetBlockedUsers;

public sealed class GetBlockedUsersQueryHandler
    : IRequestHandler<GetBlockedUsersQuery, List<BlockedUserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBlockedUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlockedUserDto>> Handle(
        GetBlockedUsersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.BlockedUsers
            .Where(b => b.UserId == request.UserId)
            .Join(
                _context.Users,
                b => b.BlockedUserId,
                u => u.Id,
                (b, u) => new BlockedUserDto(
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.AvatarUrl))
            .ToListAsync(cancellationToken);
    }
}
