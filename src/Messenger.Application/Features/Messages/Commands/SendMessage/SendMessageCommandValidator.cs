using FluentValidation;
using Messenger.Domain.Enums;

namespace Messenger.Application.Features.Messages.Commands.SendMessage;

/// <summary>Validates <see cref="SendMessageCommand"/>.</summary>
public sealed class SendMessageCommandValidator
    : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("ChatId is required.");

        RuleFor(x => x.SenderId)
            .NotEmpty()
            .WithMessage("SenderId is required.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid message type.");

        // Text messages require Content
        When(x => x.Type == MessageType.Text, () =>
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required for text messages.")
                .MaximumLength(4096)
                .WithMessage("Content must not exceed 4096 characters.");
        });

        // Image/Audio messages require MediaUrl
        When(x => x.Type == MessageType.Image || x.Type == MessageType.Audio, () =>
        {
            RuleFor(x => x.MediaUrl)
                .NotEmpty()
                .WithMessage("MediaUrl is required for image and audio messages.")
                .MaximumLength(2048)
                .WithMessage("MediaUrl must not exceed 2048 characters.");
        });
    }
}
