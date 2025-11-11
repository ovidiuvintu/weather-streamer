namespace WeatherStreamer.Domain.Enums;

/// <summary>
/// Execution status of a weather simulation.
/// </summary>
public enum SimulationStatus
{
    /// <summary>
    /// Simulation has been created but not yet begun execution.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Simulation is currently running.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Simulation has finished execution.
    /// </summary>
    Completed = 2
}
