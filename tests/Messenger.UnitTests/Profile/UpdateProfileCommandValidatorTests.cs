using Bogus;
using FluentAssertions;
using FluentValidation;
using Messenger.Application.Features.Profile.Commands.UpdateProfile;
using Xunit;

namespace Messenger.UnitTests.Profile;

/// <summary>
/// Unit tests for <see cref="UpdateProfileCommandValidator"/>.
/// Uses Bogus to generate realistic names.
/// </summary>
public sealed class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();
    private static readonly Faker<UpdateProfileCommand> CommandFaker =
        new Faker<UpdateProfileCommand>()
            .CustomInstantiator(f => new UpdateProfileCommand(
                UserId: Guid.NewGuid(),
                FirstName: f.Name.FirstName(),
                LastName: f.Name.LastName(),
                AvatarUrl: f.Internet.Avatar()));

    [Fact(DisplayName = "50 Bogus-generated profile commands all pass validation")]
    public async Task Validate_BogusProfiles_AllPass()
    {
        var commands = CommandFaker.Generate(50);

        foreach (var cmd in commands)
        {
            var result = await _validator.ValidateAsync(cmd);
            result.IsValid.Should().BeTrue(
                because: $"Bogus-generated profile '{cmd.FirstName}' should be valid");
        }
    }

    [Fact(DisplayName = "Empty FirstName fails validation")]
    public async Task Validate_EmptyFirstName_Fails()
    {
        var cmd = new UpdateProfileCommand(Guid.NewGuid(), "", null, null);
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact(DisplayName = "FirstName exceeding 64 chars fails validation")]
    public async Task Validate_FirstNameTooLong_Fails()
    {
        var longName = new string('A', 65);
        var cmd = new UpdateProfileCommand(Guid.NewGuid(), longName, null, null);
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "FirstName" &&
            e.ErrorMessage.Contains("64"));
    }

    [Fact(DisplayName = "Invalid AvatarUrl (non-URI) fails validation")]
    public async Task Validate_InvalidAvatarUrl_Fails()
    {
        var cmd = new UpdateProfileCommand(Guid.NewGuid(), "Ivan", null, "not-a-url");
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AvatarUrl");
    }

    [Fact(DisplayName = "Null AvatarUrl is accepted (optional field)")]
    public async Task Validate_NullAvatarUrl_Passes()
    {
        var cmd = new UpdateProfileCommand(Guid.NewGuid(), "Ivan", "Petrov", null);
        var result = await _validator.ValidateAsync(cmd);

        result.IsValid.Should().BeTrue();
    }
}
