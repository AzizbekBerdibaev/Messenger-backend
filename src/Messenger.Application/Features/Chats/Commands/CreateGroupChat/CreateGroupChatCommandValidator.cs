using FluentValidation;

namespace Messenger.Application.Features.Chats.Commands.CreateGroupChat;

/// <summary>Validates <see cref="CreateGroupChatCommand"/>.</summary>
internal sealed class CreateGroupChatCommandValidator
    : AbstractValidator<CreateGroupChatCommand>
{
    public CreateGroupChatCommandValidator()
    {
        RuleFor(x => x.CreatorId)
            .NotEmpty()
            .WithMessage("CreatorId is required.");

        RuleFor(x => x.ParticipantIds)
            .NotNull()
            .WithMessage("ParticipantIds is required.")
            .Must(ids => ids is { Count: >= 1 })
            .WithMessage("A group chat must have at least one other participant.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("ParticipantIds must not contain duplicates.");
    }
}
