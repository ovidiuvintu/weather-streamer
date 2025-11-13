namespace WeatherStreamer.Api.Models;

/// <summary>
/// API request model for partial simulation updates.
/// All properties are optional; only provided fields will be updated.
/// </summary>
public class UpdateSimulationRequest
{
    public string? Name { get; set; }
    public string? StartTime { get; set; }
    public string? DataSource { get; set; }
    public string? Status { get; set; }
}
