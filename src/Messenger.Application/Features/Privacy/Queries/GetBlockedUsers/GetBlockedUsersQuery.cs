using MediatR;

namespace Messenger.Application.Features.Privacy.Queries.GetBlockedUsers;

public sealed record GetBlockedUsersQuery(Guid UserId) : IRequest<List<BlockedUserDto>>;

public sealed record BlockedUserDto(Guid UserId, string FirstName, string? LastName, string? AvatarUrl);
