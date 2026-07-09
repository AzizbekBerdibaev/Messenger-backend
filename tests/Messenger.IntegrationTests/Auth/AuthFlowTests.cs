using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bogus;
using FluentAssertions;
using Messenger.Domain.Entities;
using Messenger.Infrastructure.Persistence;
using Messenger.IntegrationTests.Infrastructure;
using Messenger.WebApi.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Messenger.IntegrationTests.Auth;

/// <summary>
/// End-to-end integration test for the authentication flow.
/// Uses real Testcontainers (PostgreSQL + Redis) and a real HTTP client via
/// <see cref="MessengerWebApplicationFactory"/>.
/// </summary>
[Collection("Integration")]
public sealed class AuthFlowTests : IClassFixture<MessengerWebApplicationFactory>
{
    private readonly MessengerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    // Bogus Faker for generating realistic phone numbers and other data
    private static readonly Faker Faker = new("en");

    public AuthFlowTests(MessengerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 1: Full OTP auth flow — new user
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "SendOtp → (peek Redis) → VerifyOtp → user created in DB, JWT returned")]
    public async Task FullAuthFlow_NewUser_CreatesUserAndReturnsJwt()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        // Generate a valid E.164 phone number: +998 + 9 random digits
        var phoneNumber = "+998" + Faker.Random.ReplaceNumbers("#########");

        // ── Act 1: Send OTP ───────────────────────────────────────────────────
        var sendResponse = await _client.PostAsJsonAsync(
            "/api/auth/send-otp",
            new SendOtpRequest(phoneNumber));

        // ── Assert 1 ──────────────────────────────────────────────────────────
        sendResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            because: "a valid phone number should trigger OTP dispatch");

        // ── Act 2: Read OTP directly from the real Redis container ────────────
        // This is the test-specific escape hatch — in production clients receive
        // the code only via SMS; here we read it from the source of truth.
        var redisConnection = _factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var db = redisConnection.GetDatabase();
        var redisKey = $"otp:{phoneNumber}";
        var storedCode = (string?)await db.StringGetAsync(redisKey);

        storedCode.Should().NotBeNullOrEmpty(
            because: "OTP should be stored in Redis with key 'otp:{phoneNumber}'");
        storedCode!.Should().MatchRegex(@"^\d{4}$",
            because: "OTP must be exactly 4 digits");

        // ── Act 3: Verify OTP ─────────────────────────────────────────────────
        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtpRequest(phoneNumber, storedCode!));

        // ── Assert 3 ──────────────────────────────────────────────────────────
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "correct OTP should authenticate the user");

        var body = await verifyResponse.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        body.Should().NotBeNull();
        body!.PhoneNumber.Should().Be(phoneNumber);
        body.IsNewUser.Should().BeTrue(because: "this is the first login for this number");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.AccessTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // ── Assert 4: Verify user persisted in PostgreSQL ─────────────────────
        using var scope = _factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedUser = await appDb.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        persistedUser.Should().NotBeNull(
            because: "VerifyOtp should have created a User row in PostgreSQL");
        persistedUser!.Id.Should().Be(body.UserId);

        // ── Assert 5: OTP consumed — Redis key deleted ────────────────────────
        var codeAfterVerify = (string?)await db.StringGetAsync(redisKey);
        codeAfterVerify.Should().BeNull(
            because: "OTP must be deleted from Redis after successful verification (replay prevention)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 2: Wrong OTP → 401
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "VerifyOtp with wrong code returns 401 and does not create user")]
    public async Task VerifyOtp_WrongCode_Returns401()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var phoneNumber = "+998" + Faker.Random.ReplaceNumbers("#########");

        await _client.PostAsJsonAsync("/api/auth/send-otp", new SendOtpRequest(phoneNumber));

        // Deliberately send wrong code
        var response = await _client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtpRequest(phoneNumber, "0000"));

        // ── Assert ────────────────────────────────────────────────────────────
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userInDb = await appDb.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        userInDb.Should().BeNull(because: "failed OTP should not create a user record");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 3: Invalid phone number → 400
    // ─────────────────────────────────────────────────────────────────────────

    [Theory(DisplayName = "SendOtp rejects invalid phone numbers")]
    [InlineData("")]                  // empty
    [InlineData("   ")]               // whitespace
    [InlineData("79991234567")]       // missing leading +
    [InlineData("+998")]             // too short
    [InlineData("+7abc1234567")]      // non-digit chars
    public async Task SendOtp_InvalidPhoneNumber_Returns400(string invalidPhone)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/send-otp",
            new SendOtpRequest(invalidPhone));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: $"'{invalidPhone}' is not a valid E.164 phone number");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 4: Token refresh flow
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Refresh endpoint issues new tokens from a valid refresh token")]
    public async Task RefreshToken_ValidToken_ReturnsNewTokenPair()
    {
        // ── Arrange: authenticate first ───────────────────────────────────────
        var phoneNumber = "+998" + Faker.Random.ReplaceNumbers("#########");
        await _client.PostAsJsonAsync("/api/auth/send-otp", new SendOtpRequest(phoneNumber));

        var redisDb = _factory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var redisKey = $"otp:{phoneNumber}";
        var code = (string?)await redisDb.StringGetAsync(redisKey);
        code.Should().NotBeNull();

        var verifyResp = await _client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtpRequest(phoneNumber, code!));
        var verifyBody = await verifyResp.Content.ReadFromJsonAsync<VerifyOtpResponse>();

        // ── Act ───────────────────────────────────────────────────────────────
        var refreshResp = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest(verifyBody!.RefreshToken));

        // ── Assert ────────────────────────────────────────────────────────────
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        refreshBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshBody.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // The new tokens should be different from the original ones
        refreshBody.AccessToken.Should().NotBe(verifyBody.AccessToken,
            because: "each refresh should produce a distinct access token");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scenario 5: Existing user re-authentication (IsNewUser = false)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Existing user re-authenticating returns IsNewUser=false")]
    public async Task VerifyOtp_ExistingUser_ReturnsIsNewUserFalse()
    {
        // ── Arrange: first login ───────────────────────────────────────────────
        var phoneNumber = "+998" + Faker.Random.ReplaceNumbers("#########");

        await _client.PostAsJsonAsync("/api/auth/send-otp", new SendOtpRequest(phoneNumber));
        var redisDb = _factory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var code1 = (string?)await redisDb.StringGetAsync($"otp:{phoneNumber}");
        await _client.PostAsJsonAsync("/api/auth/verify-otp", new VerifyOtpRequest(phoneNumber, code1!));

        // ── Act: second login ──────────────────────────────────────────────────
        await _client.PostAsJsonAsync("/api/auth/send-otp", new SendOtpRequest(phoneNumber));
        var code2 = (string?)await redisDb.StringGetAsync($"otp:{phoneNumber}");
        var resp2 = await _client.PostAsJsonAsync(
            "/api/auth/verify-otp",
            new VerifyOtpRequest(phoneNumber, code2!));

        // ── Assert ────────────────────────────────────────────────────────────
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await resp2.Content.ReadFromJsonAsync<VerifyOtpResponse>();
        body2!.IsNewUser.Should().BeFalse(because: "the user already exists in the database");
    }
}
