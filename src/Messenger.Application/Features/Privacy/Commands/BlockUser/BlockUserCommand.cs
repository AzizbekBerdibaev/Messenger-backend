using MediatR;

namespace Messenger.Application.Features.Privacy.Commands.BlockUser;

public sealed record BlockUserCommand(Guid UserId, Guid BlockedUserId) : IRequest<Unit>;
