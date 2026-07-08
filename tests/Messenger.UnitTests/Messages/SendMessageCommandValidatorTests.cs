using FluentAssertions;
using FluentValidation.TestHelper;
using Messenger.Application.Features.Messages.Commands.SendMessage;
using Messenger.Domain.Enums;
using Xunit;

namespace Messenger.UnitTests.Messages;

/// <summary>
/// Unit tests for <see cref="SendMessageCommandValidator"/>.
/// </summary>
public sealed class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Valid commands
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Valid text message command passes validation")]
    public void ValidTextMessage_ShouldPassValidation()
    {
        var command = new SendMessageCommand(
            ChatId: Guid.NewGuid(),
            SenderId: Guid.NewGuid(),
            Type: MessageType.Text,
            Content: "Hello, world!",
            MediaUrl: null,
            Duration: null,
            ReplyToMessageId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Valid image message command passes validation")]
    public void ValidImageMessage_ShouldPassValidation()
    {
        var command = new SendMessageCommand(
            ChatId: Guid.NewGuid(),
            SenderId: Guid.NewGuid(),
            Type: MessageType.Image,
            Content: null,
            MediaUrl: "https://cdn.example.com/img.jpg",
            Duration: null,
            ReplyToMessageId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Invalid commands
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Empty ChatId fails validation")]
    public void EmptyChatId_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.Empty, Guid.NewGuid(), MessageType.Text, "Hi", null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.ChatId);
    }

    [Fact(DisplayName = "Empty SenderId fails validation")]
    public void EmptySenderId_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.NewGuid(), Guid.Empty, MessageType.Text, "Hi", null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.SenderId);
    }

    [Fact(DisplayName = "Text message with null Content fails validation")]
    public void TextMessageNullContent_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.NewGuid(), Guid.NewGuid(), MessageType.Text, null, null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact(DisplayName = "Text message with Content exceeding 4096 chars fails validation")]
    public void TextMessageTooLongContent_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.NewGuid(), Guid.NewGuid(), MessageType.Text,
            new string('x', 4097), null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact(DisplayName = "Image message with null MediaUrl fails validation")]
    public void ImageMessageNullMediaUrl_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.NewGuid(), Guid.NewGuid(), MessageType.Image, null, null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }

    [Fact(DisplayName = "Audio message with null MediaUrl fails validation")]
    public void AudioMessageNullMediaUrl_ShouldFailValidation()
    {
        var command = new SendMessageCommand(
            Guid.NewGuid(), Guid.NewGuid(), MessageType.Audio, null, null, null, null);

        _validator.TestValidate(command)
            .ShouldHaveValidationErrorFor(x => x.MediaUrl);
    }
}
