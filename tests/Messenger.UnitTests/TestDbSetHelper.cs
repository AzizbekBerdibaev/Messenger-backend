using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace Messenger.UnitTests;

/// <summary>
/// Shared helper for creating in-memory queryable DbSet mocks for unit tests.
/// Supports async LINQ operations (FirstOrDefaultAsync, ToListAsync, AnyAsync, CountAsync).
/// </summary>
internal static class TestDbSetHelper
{
    /// <summary>
    /// Creates a <see cref="DbSet{T}"/> mock backed by the provided <paramref name="data"/> list.
    /// </summary>
    internal static DbSet<T> CreateMockDbSet<T>(IList<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new DbSetAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new DbSetAsyncQueryProvider<T>(queryable.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        return mockSet.Object;
    }

}

// ── Async LINQ helpers (named uniquely to avoid conflicts with per-file helpers) ──

internal sealed class DbSetAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;
    internal DbSetAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new DbSetAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new DbSetAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);

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

internal sealed class DbSetAsyncEnumerable<T>
    : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public DbSetAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public DbSetAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new DbSetAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new DbSetAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal sealed class DbSetAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public DbSetAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
}
