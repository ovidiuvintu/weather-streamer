namespace WeatherStreamer.Application.DTOs;

/// <summary>
/// DTO representing a simulation read model for listing and single retrieval.
/// Note: API layer may project to its own outward DTO if property casing differs.
/// </summary>
public class SimulationListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Base64-encoded concurrency token (rowversion) for optimistic concurrency.
    /// Used to populate ETag headers in API responses.
    /// </summary>
    public string? ETag { get; set; }
}
