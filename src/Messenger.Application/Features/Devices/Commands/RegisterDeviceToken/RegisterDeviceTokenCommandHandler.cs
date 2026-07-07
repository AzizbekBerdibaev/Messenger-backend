using MediatR;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Devices.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : IRequestHandler<RegisterDeviceTokenCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public RegisterDeviceTokenCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existingDevice = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == request.UserId && d.DeviceToken == request.DeviceToken, cancellationToken);

        if (existingDevice is null)
        {
            var device = UserDevice.Create(request.UserId, request.DeviceToken, request.DeviceType);
            _context.UserDevices.Add(device);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
