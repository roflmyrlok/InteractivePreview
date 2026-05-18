using System.Net;
using System.Text.Json;
using SourceRegistryService.Domain.Exceptions;

namespace SourceRegistryService.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception");
        var response = context.Response;
        response.ContentType = "application/json";
        var (status, message) = GetErrorDetails(exception);
        response.StatusCode = (int)status;
        var result = JsonSerializer.Serialize(new { StatusCode = response.StatusCode, Message = message });
        await response.WriteAsync(result);
    }

    private static (HttpStatusCode, string) GetErrorDetails(Exception ex) => ex switch
    {
        DomainException d => (HttpStatusCode.BadRequest, d.Message),
        UnauthorizedAccessException u => (HttpStatusCode.Unauthorized, u.Message),
        KeyNotFoundException k => (HttpStatusCode.NotFound, k.Message),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
    };
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomErrorHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
