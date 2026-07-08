using Bogus;
using FluentAssertions;
using FluentValidation;
using Messenger.Application.Features.Auth.Commands.SendOtp;
using Xunit;

namespace Messenger.UnitTests.Auth;

/// <summary>
/// Unit tests for <see cref="SendOtpCommandValidator"/> using Bogus-generated data.
/// </summary>
public sealed class SendOtpCommandValidatorTests
{
    private readonly SendOtpCommandValidator _validator = new();
    private static readonly Faker Faker = new("en");

    // ── Valid inputs ─────────────────────────────────────────────────────────

    [Theory(DisplayName = "Valid E.164 phone numbers pass validation")]
    [InlineData("+79991234567")]
    [InlineData("+12025550123")]
    [InlineData("+447911123456")]
    [InlineData("+491234567890")]
    public async Task Validate_ValidE164_Passes(string phone)
    {
        var command = new SendOtpCommand(phone);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "50 randomly generated valid E.164 numbers all pass")]
    public async Task Validate_BogusGeneratedPhones_AllPass()
    {
        // Generate 50 valid-looking E.164 numbers: +7 + 10 digits
        var phones = Enumerable.Range(0, 50)
            .Select(_ => "+7" + Faker.Random.ReplaceNumbers("##########"))
            .ToList();

        foreach (var phone in phones)
        {
            var command = new SendOtpCommand(phone);
            var result = await _validator.ValidateAsync(command);

            result.IsValid.Should().BeTrue(
                because: $"'{phone}' is a valid E.164 number");
        }
    }

    // ── Invalid inputs ───────────────────────────────────────────────────────

    [Theory(DisplayName = "Invalid phone numbers fail validation")]
    [InlineData("", "Phone number is required.")]
    [InlineData("   ", "Phone number is required.")]
    [InlineData("79991234567", "Phone number must be in E.164 format")]
    [InlineData("+7999", "Phone number must be in E.164 format")]
    [InlineData("+7abc1234567", "Phone number must be in E.164 format")]
    [InlineData("+", "Phone number must be in E.164 format")]
    public async Task Validate_InvalidPhone_Fails(string phone, string expectedErrorFragment)
    {
        var command = new SendOtpCommand(phone);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains(expectedErrorFragment, StringComparison.OrdinalIgnoreCase));
    }
}
