using MediatR;

namespace Messenger.Application.Features.Search.Queries.SearchUsers;

public sealed record SearchUsersQuery(string SearchTerm) : IRequest<List<UserSearchResultDto>>;

public sealed record UserSearchResultDto(Guid UserId, string FirstName, string? LastName, string? AvatarUrl);
