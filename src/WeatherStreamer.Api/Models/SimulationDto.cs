namespace WeatherStreamer.Api.Models;

/// <summary>
/// API DTO for simulation retrieval with required casing.
/// </summary>
public class SimulationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // ISO 8601 UTC string
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
