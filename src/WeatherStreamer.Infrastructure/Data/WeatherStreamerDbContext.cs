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
    
    /// <summary>
    /// Audit entries for changes to simulations.
    /// </summary>
    public DbSet<WeatherStreamer.Domain.Entities.AuditEntry> AuditEntries => Set<WeatherStreamer.Domain.Entities.AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configuration for Simulation and AuditEntry
        modelBuilder.ApplyConfiguration(new SimulationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AuditEntryConfiguration());
    }
}
