using FluentValidation;

namespace Messenger.Application.Features.Stories.Commands.PublishStory;

/// <summary>Validates <see cref="PublishStoryCommand"/>.</summary>
public sealed class PublishStoryCommandValidator
    : AbstractValidator<PublishStoryCommand>
{
    public PublishStoryCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.MediaUrl)
            .NotEmpty()
            .WithMessage("MediaUrl is required.")
            .MaximumLength(2048)
            .WithMessage("MediaUrl must not exceed 2048 characters.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("MediaUrl must be a valid absolute URI.");
    }
}
