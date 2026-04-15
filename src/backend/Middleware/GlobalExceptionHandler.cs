using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MGSPlus.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns RFC 7807 ProblemDetails JSON.
/// Prevents stack traces from leaking to clients.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env    = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path}",
            ctx.Request.Method,
            ctx.Request.Path);

        var (status, title) = exception switch
        {
            KeyNotFoundException  => (StatusCodes.Status404NotFound,      "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ArgumentException     => (StatusCodes.Status400BadRequest,    "Invalid argument"),
            InvalidOperationException  => (StatusCodes.Status400BadRequest, "Invalid operation"),
            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Client closed request"),
            _                     => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title  = title,
            // Only expose detail in development
            Detail = _env.IsDevelopment() ? exception.Message : null,
            Instance = ctx.Request.Path
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
