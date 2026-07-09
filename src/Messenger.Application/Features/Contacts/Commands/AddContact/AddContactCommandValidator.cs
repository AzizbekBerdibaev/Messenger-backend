using FluentValidation;

namespace Messenger.Application.Features.Contacts.Commands.AddContact;

public sealed class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    private const string E164Pattern = @"^\+998\d{9}$";

    public AddContactCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(E164Pattern)
            .WithMessage("Phone number must be in format +998xxxxxxxxx.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.LastName)
            .MaximumLength(64)
            .When(x => x.LastName is not null);
    }
}
