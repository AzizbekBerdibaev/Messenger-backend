using FluentValidation;

namespace Messenger.Application.Features.Auth.Commands.SendOtp;

/// <summary>
/// Validates <see cref="SendOtpCommand"/> before it reaches the handler.
/// Phone number must be in E.164 format: +[country code][number], e.g. +79991234567.
/// </summary>
public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    // E.164 regex: starts with +, followed by 7–15 digits
    private const string E164Pattern = @"^\+[1-9]\d{6,14}$";

    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(E164Pattern)
            .WithMessage("Phone number must be in E.164 format (e.g. +79991234567).");
    }
}
