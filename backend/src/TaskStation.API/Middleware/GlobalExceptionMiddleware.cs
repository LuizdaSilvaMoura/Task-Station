using System.Net;
using System.Text.Json;
using TaskStation.Domain.Exceptions;

namespace TaskStation.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            TaskValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Validation Error", validationEx.Errors)
            ),
            EntityNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(notFoundEx.Message)
            ),
            DomainException domainEx => (
                HttpStatusCode.UnprocessableEntity,
                new ErrorResponse(domainEx.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("An unexpected error occurred.")
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(exception, "Domain/validation error: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

internal sealed record ErrorResponse
{
    public string Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public ErrorResponse(string message, IReadOnlyList<string>? errors = null)
    {
        Message = message;
        Errors = errors;
    }
}
