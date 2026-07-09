using FluentValidation;

namespace Messenger.Application.Features.Auth.Commands.SendOtp;

/// <summary>
/// Validates <see cref="SendOtpCommand"/> before it reaches the handler.
/// Phone number must be in E.164 format: +[country code][number], e.g. +79991234567.
/// </summary>
public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    // Uzbekistan format: +998 followed by 9 digits
    private const string E164Pattern = @"^\+998\d{9}$";

    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(E164Pattern)
            .WithMessage("Phone number must be in format +998xxxxxxxxx.");
    }
}
