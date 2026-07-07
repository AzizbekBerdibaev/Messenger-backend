using MediatR;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Search.Queries.SearchUsers;

public sealed class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, List<UserSearchResultDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserSearchResultDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm.ToLower();

        return await _context.Users
            .Where(u => u.PhoneNumber.Contains(term) || 
                        u.FirstName.ToLower().Contains(term) || 
                        (u.LastName != null && u.LastName.ToLower().Contains(term)))
            .Take(50)
            .Select(u => new UserSearchResultDto(u.Id, u.FirstName, u.LastName, u.AvatarUrl))
            .ToListAsync(cancellationToken);
    }
}
