using FluentValidation;

namespace Messenger.Application.Features.Messages.Commands.MarkMessagesRead;

/// <summary>Validates <see cref="MarkMessagesReadCommand"/>.</summary>
internal sealed class MarkMessagesReadCommandValidator
    : AbstractValidator<MarkMessagesReadCommand>
{
    public MarkMessagesReadCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("ChatId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.UpToMessageId)
            .NotEmpty()
            .WithMessage("UpToMessageId is required.");
    }
}
