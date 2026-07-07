using FluentValidation;

namespace Messenger.Application.Features.Chats.Commands.CreatePrivateChat;

/// <summary>Validates <see cref="CreatePrivateChatCommand"/>.</summary>
internal sealed class CreatePrivateChatCommandValidator
    : AbstractValidator<CreatePrivateChatCommand>
{
    public CreatePrivateChatCommandValidator()
    {
        RuleFor(x => x.CurrentUserId)
            .NotEmpty()
            .WithMessage("CurrentUserId is required.");

        RuleFor(x => x.OtherUserId)
            .NotEmpty()
            .WithMessage("OtherUserId is required.")
            .NotEqual(x => x.CurrentUserId)
            .WithMessage("Cannot create a private chat with yourself.");
    }
}
