using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items["CorrelationId"] as string ?? httpContext.TraceIdentifier;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ExceptionType"] = exception.GetType().Name
        }))
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }

        var (statusCode, title, detail) = exception switch
        {
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "The request was invalid."),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action."),

            KeyNotFoundException or ArgumentNullException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                "The requested resource was not found."),

            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "The operation could not be completed due to a conflict."),

            _ => env.IsDevelopment()
                ? (StatusCodes.Status500InternalServerError, "Server Error", exception.Message)
                : (StatusCodes.Status500InternalServerError, "Server Error",
                    "An unexpected error occurred. Please try again later.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
                ["correlationId"] = correlationId
            }
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
