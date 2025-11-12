using WeatherStreamer.Application.DTOs;

namespace WeatherStreamer.Application.Services.Simulations;

/// <summary>
/// Read-only service for retrieving simulations (Clean Architecture: Application layer).
/// </summary>
public interface ISimulationReadService
{
    /// <summary>
    /// Retrieve all simulations ordered by StartTime asc, then Id asc.
    /// </summary>
    Task<IReadOnlyList<SimulationListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve simulations whose StartTime is greater than or equal to the given UTC boundary.
    /// </summary>
    Task<IReadOnlyList<SimulationListItem>> GetFromStartTimeAsync(DateTime boundaryUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a single simulation by id.
    /// Returns null if not found.
    /// </summary>
    Task<SimulationListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
