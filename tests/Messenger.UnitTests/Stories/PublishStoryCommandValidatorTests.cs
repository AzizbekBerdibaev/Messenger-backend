using FluentValidation.TestHelper;
using Messenger.Application.Features.Stories.Commands.PublishStory;
using Xunit;

namespace Messenger.UnitTests.Stories;

/// <summary>
/// Unit tests for <see cref="PublishStoryCommandValidator"/>.
/// </summary>
public sealed class PublishStoryCommandValidatorTests
{
    private readonly PublishStoryCommandValidator _validator = new();

    [Fact(DisplayName = "Valid command passes validation")]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new PublishStoryCommand(
            UserId: Guid.NewGuid(),
            MediaUrl: "https://cdn.example.com/story.mp4");

        _validator.TestValidate(command)
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Empty UserId fails validation")]
    public void EmptyUserId_ShouldFailValidation()
    {
        var command = new PublishStoryCommand(
            UserId: Guid.Empty,
            MediaUrl: "https://cdn.example.com/story.mp4");

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact(DisplayName = "Empty MediaUrl fails validation")]
    public void EmptyMediaUrl_ShouldFailValidation()
    {
        var command = new PublishStoryCommand(Guid.NewGuid(), string.Empty);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }

    [Fact(DisplayName = "Relative MediaUrl fails validation")]
    public void RelativeMediaUrl_ShouldFailValidation()
    {
        var command = new PublishStoryCommand(Guid.NewGuid(), "/relative/path/image.jpg");

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }

    [Fact(DisplayName = "MediaUrl exceeding 2048 chars fails validation")]
    public void TooLongMediaUrl_ShouldFailValidation()
    {
        var command = new PublishStoryCommand(
            Guid.NewGuid(),
            "https://example.com/" + new string('a', 2050));

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }
}
