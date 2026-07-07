using FluentValidation;

namespace Messenger.Application.Features.Chats.Queries.GetChatMessages;

/// <summary>Validates <see cref="GetChatMessagesQuery"/>.</summary>
internal sealed class GetChatMessagesQueryValidator
    : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("ChatId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100.");
    }
}
