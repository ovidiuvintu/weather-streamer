namespace WeatherStreamer.Domain.Entities;

/// <summary>
/// Represents an audit trail entry for changes made to a Simulation.
/// </summary>
public class AuditEntry
{
    public int Id { get; set; }

    /// <summary>
    /// The simulation id that was changed.
    /// </summary>
    public int SimulationId { get; set; }

    /// <summary>
    /// Actor who performed the change (username or 'anonymous').
    /// </summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>
    /// Optional correlation id for traceability.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// UTC timestamp when the change occurred.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// JSON-serialized changes (list of { field,before,after }).
    /// </summary>
    public string ChangesJson { get; set; } = string.Empty;

    /// <summary>
    /// Previous ETag (base64) before the change.
    /// </summary>
    public string? PrevETag { get; set; }

    /// <summary>
    /// New ETag (base64) after the change.
    /// </summary>
    public string? NewETag { get; set; }
}
