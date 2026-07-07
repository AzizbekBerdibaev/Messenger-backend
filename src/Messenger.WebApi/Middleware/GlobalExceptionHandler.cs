using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.WebApi.Middleware;

/// <summary>
/// Global exception handler that maps domain/application exceptions to structured
/// ProblemDetails HTTP responses. All unhandled errors return 500 by default.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext      httpContext,
        Exception        exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            ValidationException          => (StatusCodes.Status400BadRequest,          "Validation Failed"),
            UnauthorizedAccessException  => (StatusCodes.Status401Unauthorized,        "Unauthorized"),
            KeyNotFoundException         => (StatusCodes.Status404NotFound,            "Not Found"),
            InvalidOperationException    => (StatusCodes.Status409Conflict,            "Conflict"),
            _                            => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        ProblemDetails problem;

        if (exception is ValidationException validationEx)
        {
            // Include per-field errors in the response
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var validationProblem = new HttpValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title  = title
            };
            problem = validationProblem;
        }
        else
        {
            problem = new ProblemDetails
            {
                Status = statusCode,
                Title  = title,
                Detail = exception.Message
            };
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // exception handled
    }
}
