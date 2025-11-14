using Microsoft.EntityFrameworkCore;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;
using System.Security.Cryptography;

namespace WeatherStreamer.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Simulation entity operations.
/// </summary>
public class SimulationRepository : ISimulationRepository
{
    private readonly WeatherStreamerDbContext _context;

    public SimulationRepository(WeatherStreamerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Simulation> CreateAsync(Simulation simulation, CancellationToken cancellationToken = default)
    {
        if (simulation == null)
        {
            throw new ArgumentNullException(nameof(simulation));
        }

        try
        {
            _context.Simulations.Add(simulation);
            // SaveChangesAsync uses implicit transaction - will rollback on exception
            await _context.SaveChangesAsync(cancellationToken);
            
            return simulation;
        }
        catch (DbUpdateException ex)
        {
            // Database constraint violation or update failure
            // Let exception propagate to service layer for logging and handling
            throw new InvalidOperationException(
                "Failed to create simulation due to a database error. This may be due to a constraint violation or connectivity issue.",
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsFileInUseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        return await _context.Simulations
            .AnyAsync(s => s.FileName == filePath && s.Status == SimulationStatus.InProgress, 
                     cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Simulation?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return await _context.Simulations.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Simulation> UpdateAsync(Simulation simulation, byte[] ifMatchRowVersion, CancellationToken cancellationToken = default)
    {
        if (simulation is null) throw new ArgumentNullException(nameof(simulation));
        if (ifMatchRowVersion is null || ifMatchRowVersion.Length == 0) throw new ArgumentException("If-Match rowversion is required", nameof(ifMatchRowVersion));

        try
        {
            // Set the original rowversion value for concurrency check
            var entry = _context.Entry(simulation);
            entry.Property(e => e.RowVersion).OriginalValue = ifMatchRowVersion;

            // Generate a new rowversion token to simulate DB-generated timestamp across providers
            // (InMemory/SQLite providers may not auto-generate rowversion values)
            simulation.RowVersion = RandomNumberGenerator.GetBytes(8);

            _context.Update(simulation);
            await _context.SaveChangesAsync(cancellationToken);
            return simulation;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Map to a domain-agnostic concurrency signal for upper layers
            throw new InvalidOperationException("Concurrency conflict detected while updating Simulation.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to update simulation due to a database error.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(int id, byte[] ifMatchRowVersion, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (ifMatchRowVersion is null || ifMatchRowVersion.Length == 0) throw new ArgumentException("If-Match rowversion is required", nameof(ifMatchRowVersion));

        var entity = await _context.Simulations.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
            return false;

        try
        {
            var entry = _context.Entry(entity);
            entry.Property(e => e.RowVersion).OriginalValue = ifMatchRowVersion;

            entity.IsDeleted = true;

            // bump RowVersion to simulate DB behavior on providers that don't auto-generate
            entity.RowVersion = RandomNumberGenerator.GetBytes(8);

            _context.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Concurrency conflict detected while deleting Simulation.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to delete simulation due to a database error.", ex);
        }
    }
}
