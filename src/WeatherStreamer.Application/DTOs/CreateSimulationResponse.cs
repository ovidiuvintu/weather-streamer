namespace WeatherStreamer.Application.DTOs;

/// <summary>
/// Response model for successful simulation creation.
/// Returns the auto-generated simulation ID.
/// </summary>
public class CreateSimulationResponse
{
    /// <summary>
    /// The unique identifier assigned to the newly created simulation.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the created simulation (echoed back for confirmation).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The start time in ISO 8601 UTC format.
    /// </summary>
    public DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// The file path of the data source.
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// The initial status (always "NotStarted" for new simulations).
    /// </summary>
    public string Status { get; set; } = "NotStarted";
}
