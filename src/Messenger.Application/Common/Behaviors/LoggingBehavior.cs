using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Messenger.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior that logs the start, completion, and elapsed time of every command/query.
/// Warnings are emitted for requests that exceed <see cref="SlowRequestThreshold"/>.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromMilliseconds(500);

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.Elapsed > SlowRequestThreshold)
            {
                _logger.LogWarning(
                    "Slow request detected: {RequestName} took {ElapsedMs} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
