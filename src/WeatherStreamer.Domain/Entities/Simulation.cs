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
    /// Soft-delete flag. When true the simulation is considered deleted and should
    /// not be returned by normal read queries.
    /// </summary>
    public bool IsDeleted { get; set; }

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

    /// <summary>
    /// Apply partial updates to this simulation and enforce status transition rules.
    /// Throws <see cref="InvalidOperationException"/> for illegal transitions or immutable field changes.
    /// </summary>
    /// <param name="newName">New name (null if no change)</param>
    /// <param name="newStartTimeUtc">New start time in UTC (null if no change)</param>
    /// <param name="newFileName">New data source path (null if no change)</param>
    /// <param name="newStatus">New status (null if no change)</param>
    public void ApplyUpdate(string? newName, DateTime? newStartTimeUtc, string? newFileName, SimulationStatus? newStatus)
    {
        // Enforce immutability for start time and file name when already started
        EnsureMutableForUpdate(newFileName, newStartTimeUtc);

        // Status transition enforcement
        if (newStatus.HasValue)
        {
            var target = newStatus.Value;
            // No-op
            if (target != Status)
            {
                // Disallow backwards transitions
                if (target < Status)
                {
                    throw new InvalidOperationException("Illegal status transition: cannot move to a previous state.");
                }

                // Disallow skipping NotStarted -> Completed
                if (Status == SimulationStatus.NotStarted && target == SimulationStatus.Completed)
                {
                    throw new InvalidOperationException("Illegal status transition: Not Started cannot jump directly to Completed.");
                }

                // All checks passed, apply status
                Status = target;
            }
        }

        if (newName is not null)
            Name = newName;

        if (newStartTimeUtc.HasValue)
            StartTime = newStartTimeUtc.Value;

        if (newFileName is not null)
            FileName = newFileName;
    }
}
