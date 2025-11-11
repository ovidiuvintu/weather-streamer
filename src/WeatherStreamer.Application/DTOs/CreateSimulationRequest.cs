namespace WeatherStreamer.Application.DTOs;

/// <summary>
/// Request model for creating a new simulation.
/// Client provides Name, StartTime, and DataSource only.
/// Status is set server-side and not included in request.
/// </summary>
public class CreateSimulationRequest
{
    /// <summary>
    /// Descriptive name of the simulation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The start time of the simulation in ISO 8601 format (e.g., "2025-11-10T14:30:00Z").
    /// Will be converted to UTC if timezone information provided.
    /// </summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the CSV data source file.
    /// </summary>
    public string DataSource { get; set; } = string.Empty;
}
