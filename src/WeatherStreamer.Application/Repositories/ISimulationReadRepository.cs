using WeatherStreamer.Domain.Entities;

namespace WeatherStreamer.Application.Repositories;

/// <summary>
/// Read-only repository for simulations.
/// </summary>
public interface ISimulationReadRepository
{
    Task<IReadOnlyList<Simulation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Simulation>> GetFromStartTimeAsync(DateTime boundaryUtc, CancellationToken cancellationToken = default);
    Task<Simulation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
