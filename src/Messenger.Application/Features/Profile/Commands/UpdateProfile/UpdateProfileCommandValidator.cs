using FluentValidation;

namespace Messenger.Application.Features.Profile.Commands.UpdateProfile;

/// <summary>Validates <see cref="UpdateProfileCommand"/>.</summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must be a valid GUID.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(64)
            .WithMessage("First name must not exceed 64 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(64)
            .WithMessage("Last name must not exceed 64 characters.")
            .When(x => x.LastName is not null);

        RuleFor(x => x.AvatarUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Avatar URL must be a valid absolute URI.")
            .When(x => x.AvatarUrl is not null);
    }
}
