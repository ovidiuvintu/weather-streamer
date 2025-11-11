namespace WeatherStreamer.Api.Models;

/// <summary>
/// Standardized error response format for all API errors.
/// Implements Constitution Principle V (centralized error handling).
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601 UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// High-level error message (e.g., "Validation failed").
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Detailed validation errors or additional context (optional).
    /// Key = field name, Value = list of error messages for that field.
    /// </summary>
    public Dictionary<string, List<string>>? Details { get; set; }
}
