using System.Net;
using System.Text.Json;
using WeatherStreamer.Api.Models;

namespace WeatherStreamer.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log with different severity based on exception type
            if (IsDatabaseError(ex))
            {
                _logger.LogCritical(ex, "Database error occurred. CorrelationId: {CorrelationId}", 
                    context.Response.Headers["X-Correlation-ID"].ToString());
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
                    context.Response.Headers["X-Correlation-ID"].ToString());
            }
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private static bool IsDatabaseError(Exception exception)
    {
        // Check if it's a database-related error
        return exception is InvalidOperationException && 
               exception.Message.Contains("database error", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        
        var errorResponse = new ErrorResponse
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Error = "Internal server error",
            Details = new Dictionary<string, List<string>>
            {
                { "Message", new List<string> { $"An unexpected error occurred. Please contact support with correlation ID: {correlationId}" } }
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
