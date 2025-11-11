using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Domain.Entities;

/// <summary>
/// Represents a weather data streaming simulation configuration.
/// </summary>
public class Simulation
{
    /// <summary>
    /// Unique identifier for the simulation (auto-generated).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Descriptive name of the simulation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The scheduled start time for the simulation in UTC.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Full path to the CSV data source file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Current execution status of the simulation.
    /// </summary>
    public SimulationStatus Status { get; set; }
}
