using FluentValidation;

namespace Messenger.Application.Features.Storage.Commands.GenerateUploadUrl;

public sealed class GenerateUploadUrlCommandValidator : AbstractValidator<GenerateUploadUrlCommand>
{
    public GenerateUploadUrlCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(100);
    }
}
