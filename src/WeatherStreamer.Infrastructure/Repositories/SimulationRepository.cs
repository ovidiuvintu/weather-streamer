using Microsoft.EntityFrameworkCore;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;

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
}
