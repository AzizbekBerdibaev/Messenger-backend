using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Privacy.Commands.BlockUser;

public sealed class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public BlockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.BlockedUserId)
            throw new InvalidOperationException("Cannot block yourself.");

        var existingBlock = await _context.BlockedUsers
            .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.BlockedUserId == request.BlockedUserId, cancellationToken);

        if (existingBlock is null)
        {
            var blockedUser = BlockedUser.Create(request.UserId, request.BlockedUserId);
            _context.BlockedUsers.Add(blockedUser);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
