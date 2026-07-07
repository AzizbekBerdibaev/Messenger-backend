using FluentValidation;

namespace Messenger.Application.Features.Devices.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.DeviceToken)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.DeviceType)
            .IsInEnum();
    }
}
