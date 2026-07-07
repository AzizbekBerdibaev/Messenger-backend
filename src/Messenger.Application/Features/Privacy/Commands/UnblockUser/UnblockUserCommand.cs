using MediatR;

namespace Messenger.Application.Features.Privacy.Commands.UnblockUser;

public sealed record UnblockUserCommand(Guid UserId, Guid BlockedUserId) : IRequest<Unit>;
