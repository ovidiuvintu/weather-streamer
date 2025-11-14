using WeatherStreamer.Domain.Entities;

namespace WeatherStreamer.Application.Repositories;

/// <summary>
/// Repository interface for Simulation entity operations.
/// </summary>
public interface ISimulationRepository
{
    /// <summary>
    /// Creates a new simulation in the database.
    /// </summary>
    /// <param name="simulation">The simulation entity to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created simulation with generated ID.</returns>
    Task<Simulation> CreateAsync(Simulation simulation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file is currently in use by an in-progress simulation.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if file is in use, false otherwise.</returns>
    Task<bool> IsFileInUseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a simulation by id for update (tracked by DbContext).
    /// Returns null if not found.
    /// </summary>
    Task<Simulation?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a simulation with optimistic concurrency using rowversion.
    /// The <paramref name="ifMatchRowVersion"/> must match the current rowversion of the entity.
    /// On success, the entity's RowVersion will be refreshed to a new value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a concurrency conflict occurs.</exception>
    Task<Simulation> UpdateAsync(Simulation simulation, byte[] ifMatchRowVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-delete a simulation by id using optimistic concurrency via rowversion.
    /// Returns true when deletion applied; false if the entity was not found.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a concurrency conflict occurs.</exception>
    Task<bool> SoftDeleteAsync(int id, byte[] ifMatchRowVersion, CancellationToken cancellationToken = default);
}
