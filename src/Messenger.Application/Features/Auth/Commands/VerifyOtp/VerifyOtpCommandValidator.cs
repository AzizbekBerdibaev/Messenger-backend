using FluentValidation;

namespace Messenger.Application.Features.Auth.Commands.VerifyOtp;

/// <summary>Validates <see cref="VerifyOtpCommand"/> before it reaches the handler.</summary>
public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    private const string E164Pattern = @"^\+[1-9]\d{6,14}$";

    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(E164Pattern)
            .WithMessage("Phone number must be in E.164 format.");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("OTP code is required.")
            .Length(4)
            .WithMessage("OTP code must be exactly 4 characters.")
            .Matches(@"^\d{4}$")
            .WithMessage("OTP code must consist of 4 digits.");
    }
}
