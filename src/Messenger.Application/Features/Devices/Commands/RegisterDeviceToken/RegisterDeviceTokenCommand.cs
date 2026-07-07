using MediatR;
using Messenger.Domain.Enums;

namespace Messenger.Application.Features.Devices.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    Guid UserId,
    string DeviceToken,
    DeviceType DeviceType) : IRequest<Unit>;
