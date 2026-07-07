using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Privacy.Commands.UnblockUser;

public sealed class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UnblockUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        var existingBlock = await _context.BlockedUsers
            .FirstOrDefaultAsync(b => b.UserId == request.UserId && b.BlockedUserId == request.BlockedUserId, cancellationToken);

        if (existingBlock is not null)
        {
            _context.BlockedUsers.Remove(existingBlock);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
