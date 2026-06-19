using LinearClone.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LinearClone.Api.Infrastructure;

// Centralizes exception->HTTP mapping so controllers don't need try/catch blocks.
// Implements IExceptionHandler (ASP.NET Core 8+). Registered in Program.cs with
// AddExceptionHandler + UseExceptionHandler. Returns RFC 7807 ProblemDetails.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, exception.Message),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        // Log unexpected (500) errors with full detail; expected ones at a lower level.
        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true; // handled
    }
}