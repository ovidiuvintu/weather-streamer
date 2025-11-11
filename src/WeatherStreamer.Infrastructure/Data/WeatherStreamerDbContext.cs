using Microsoft.EntityFrameworkCore;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Infrastructure.Data.Configurations;

namespace WeatherStreamer.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for the Weather Streamer application.
/// </summary>
public class WeatherStreamerDbContext : DbContext
{
    public WeatherStreamerDbContext(DbContextOptions<WeatherStreamerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Simulations table.
    /// </summary>
    public DbSet<Simulation> Simulations => Set<Simulation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfiguration(new SimulationConfiguration());
    }
}
