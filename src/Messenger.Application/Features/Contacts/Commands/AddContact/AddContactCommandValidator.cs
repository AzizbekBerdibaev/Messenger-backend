using FluentValidation;

namespace Messenger.Application.Features.Contacts.Commands.AddContact;

public sealed class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    private const string E164Pattern = @"^\+[1-9]\d{6,14}$";

    public AddContactCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(E164Pattern)
            .WithMessage("Phone number must be in E.164 format.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.LastName)
            .MaximumLength(64)
            .When(x => x.LastName is not null);
    }
}
