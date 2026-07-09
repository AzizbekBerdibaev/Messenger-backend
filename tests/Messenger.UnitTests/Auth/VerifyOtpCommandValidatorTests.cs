using Bogus;
using FluentAssertions;
using FluentValidation;
using Messenger.Application.Features.Auth.Commands.VerifyOtp;
using Xunit;

namespace Messenger.UnitTests.Auth;

/// <summary>
/// Unit tests for <see cref="VerifyOtpCommandValidator"/>.
/// </summary>
public sealed class VerifyOtpCommandValidatorTests
{
    private readonly VerifyOtpCommandValidator _validator = new();

    [Theory(DisplayName = "Valid OTP codes (4 digits) pass validation")]
    [InlineData("0000")]
    [InlineData("1234")]
    [InlineData("9999")]
    public async Task Validate_ValidCode_Passes(string code)
    {
        var result = await _validator.ValidateAsync(new VerifyOtpCommand("+998901234567", code));
        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "Invalid OTP codes fail validation")]
    [InlineData("", "OTP code is required.")]
    [InlineData("123", "OTP code must be exactly 4 characters.")]
    [InlineData("12345", "OTP code must be exactly 4 characters.")]
    [InlineData("abcd", "OTP code must consist of 4 digits.")]
    [InlineData("12 4", "OTP code must consist of 4 digits.")]
    public async Task Validate_InvalidCode_Fails(string code, string expectedError)
    {
        var result = await _validator.ValidateAsync(new VerifyOtpCommand("+998901234567", code));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains(expectedError, StringComparison.OrdinalIgnoreCase));
    }
}
