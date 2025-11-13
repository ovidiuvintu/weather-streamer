namespace WeatherStreamer.Application.Services.Simulations.Update;

/// <summary>
/// Command representing a partial update to a Simulation.
/// Only provided properties are updated.
/// </summary>
public class UpdateSimulationCommand
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? StartTime { get; init; }
    public string? DataSource { get; init; }
    public string? Status { get; init; }
    /// <summary>
    /// Base64 encoded rowversion token supplied via If-Match header.
    /// </summary>
    public string IfMatch { get; init; } = string.Empty;
    /// <summary>
    /// Optional actor performing the update (e.g. username). Defaults to null meaning anonymous.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Correlation id for request tracing; if provided it will be included in audit logs.
    /// </summary>
    public string? CorrelationId { get; init; }
}
