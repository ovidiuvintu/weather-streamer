using WeatherStreamer.Domain.Enums;
using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Concurrency token used for optimistic concurrency control.
    /// Exposed as ETag in API responses.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Ensure that the provided candidate updates are allowed for the current simulation state.
    /// Throws <see cref="InvalidOperationException"/> if the update attempts to change immutable fields
    /// after the simulation has left the NotStarted state.
    /// </summary>
    /// <param name="newFileName">Candidate file name (null if not changing)</param>
    /// <param name="newStartTimeUtc">Candidate start time in UTC (null if not changing)</param>
    public void EnsureMutableForUpdate(string? newFileName, DateTime? newStartTimeUtc)
    {
        if (Status == Enums.SimulationStatus.NotStarted)
            return;

        if (newFileName is not null && !string.Equals(newFileName, FileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot change DataSource after the simulation has started.");
        }

        if (newStartTimeUtc.HasValue && newStartTimeUtc.Value != StartTime)
        {
            throw new InvalidOperationException("Cannot change StartTime after the simulation has started.");
        }
    }
}
