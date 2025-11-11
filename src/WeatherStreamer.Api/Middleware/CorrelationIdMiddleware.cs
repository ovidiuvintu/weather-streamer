using Serilog.Context;

namespace WeatherStreamer.Api.Middleware;

/// <summary>
/// Middleware to generate and track correlation IDs for each request.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        // Add to response headers
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);
        
        // Push to Serilog log context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
