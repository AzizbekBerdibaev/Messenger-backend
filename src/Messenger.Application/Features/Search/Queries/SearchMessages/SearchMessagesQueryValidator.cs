using FluentValidation;

namespace Messenger.Application.Features.Search.Queries.SearchMessages;

public sealed class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
{
    public SearchMessagesQueryValidator()
    {
        RuleFor(x => x.ChatId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(3).MaximumLength(50);
    }
}
