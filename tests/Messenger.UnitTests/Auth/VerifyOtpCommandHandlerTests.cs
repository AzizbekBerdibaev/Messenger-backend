using Bogus;
using FluentAssertions;
using Messenger.Application.Common.Interfaces;
using Messenger.Application.Features.Auth.Commands.VerifyOtp;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Messenger.UnitTests.Auth;

/// <summary>
/// Unit tests for <see cref="VerifyOtpCommandHandler"/> with all dependencies mocked.
/// </summary>
public sealed class VerifyOtpCommandHandlerTests
{
    private static readonly Faker Faker = new("en");

    private readonly Mock<IOtpService> _otpServiceMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly Mock<ILogger<VerifyOtpCommandHandler>> _loggerMock = new();

    private VerifyOtpCommandHandler CreateHandler() => new(
        _otpServiceMock.Object,
        _jwtServiceMock.Object,
        _dbMock.Object,
        _loggerMock.Object);

    [Fact(DisplayName = "Valid OTP for new user creates user and returns IsNewUser=true")]
    public async Task Handle_ValidOtp_NewUser_CreatesUserAndReturnsTokens()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var phone = "+7" + Faker.Random.ReplaceNumbers("##########");
        var command = new VerifyOtpCommand(phone, "1234");

        // OTP validates successfully
        _otpServiceMock
            .Setup(s => s.ValidateAsync(phone, "1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // No existing user in DB
        var emptyUserSet = CreateMockDbSet<User>([]);
        _dbMock.Setup(d => d.Users).Returns(emptyUserSet);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // JWT service returns deterministic tokens
        var expectedTokens = new TokenPair(
            AccessToken: "access-token",
            RefreshToken: "refresh-token",
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15));

        _jwtServiceMock
            .Setup(j => j.GenerateTokenPair(It.IsAny<Guid>(), phone))
            .Returns(expectedTokens);

        // ── Act ───────────────────────────────────────────────────────────────
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.IsNewUser.Should().BeTrue();
        result.PhoneNumber.Should().Be(phone);
        result.Tokens.AccessToken.Should().Be("access-token");
        result.Tokens.RefreshToken.Should().Be("refresh-token");

        _dbMock.Verify(d => d.Users.Add(It.Is<User>(u => u.PhoneNumber == phone)), Times.Once);
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Valid OTP for existing user does NOT create user and returns IsNewUser=false")]
    public async Task Handle_ValidOtp_ExistingUser_DoesNotCreateAndReturnsIsNewUserFalse()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var phone = "+7" + Faker.Random.ReplaceNumbers("##########");
        var existingUser = User.Create(Guid.NewGuid(), phone);
        var command = new VerifyOtpCommand(phone, "5678");

        _otpServiceMock
            .Setup(s => s.ValidateAsync(phone, "5678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var existingUserSet = CreateMockDbSet([existingUser]);
        _dbMock.Setup(d => d.Users).Returns(existingUserSet);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _jwtServiceMock
            .Setup(j => j.GenerateTokenPair(existingUser.Id, phone))
            .Returns(new TokenPair("a", "r", DateTime.UtcNow.AddMinutes(15)));

        // ── Act ───────────────────────────────────────────────────────────────
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        result.IsNewUser.Should().BeFalse();
        result.UserId.Should().Be(existingUser.Id);

        // Must NOT call Add on the DbSet
        _dbMock.Verify(d => d.Users.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact(DisplayName = "Invalid OTP throws UnauthorizedAccessException")]
    public async Task Handle_InvalidOtp_ThrowsUnauthorized()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var phone = "+7" + Faker.Random.ReplaceNumbers("##########");
        var command = new VerifyOtpCommand(phone, "0000");

        _otpServiceMock
            .Setup(s => s.ValidateAsync(phone, "0000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // ── Act & Assert ──────────────────────────────────────────────────────
        var handler = CreateHandler();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DbSet<T> CreateMockDbSet<T>(IList<T> data)
        where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return mockSet.Object;
    }
}

// ── Async LINQ test helpers ──────────────────────────────────────────────────

internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression)
        => _inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(
        System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(System.Linq.Expressions.Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
