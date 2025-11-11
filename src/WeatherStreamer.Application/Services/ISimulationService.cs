using WeatherStreamer.Application.DTOs;

namespace WeatherStreamer.Application.Services;

/// <summary>
/// Service interface for simulation business logic.
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// Creates a new simulation with validation and persistence.
    /// </summary>
    /// <param name="request">The simulation creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created simulation.</returns>
    Task<int> CreateSimulationAsync(CreateSimulationRequest request, CancellationToken cancellationToken = default);
}
