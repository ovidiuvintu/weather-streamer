using Microsoft.EntityFrameworkCore;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.Infrastructure.Repositories;

/// <summary>
/// Read-only repository implementation for simulations using EF Core.
/// </summary>
public class SimulationReadRepository : ISimulationReadRepository
{
    private readonly WeatherStreamerDbContext _context;

    public SimulationReadRepository(WeatherStreamerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Simulation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Simulations
            .AsNoTracking()
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Simulation>> GetFromStartTimeAsync(DateTime boundaryUtc, CancellationToken cancellationToken = default)
    {
        // assume StartTime stored in UTC; compare directly
        return await _context.Simulations
            .AsNoTracking()
            .Where(s => s.StartTime >= boundaryUtc)
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Simulation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Simulations
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
