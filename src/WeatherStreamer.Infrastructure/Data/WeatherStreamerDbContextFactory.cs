using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeatherStreamer.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core tools to create WeatherStreamerDbContext without starting the web host.
/// This avoids executing Program.cs during migrations add/update commands.
/// </summary>
public class WeatherStreamerDbContextFactory : IDesignTimeDbContextFactory<WeatherStreamerDbContext>
{
    public WeatherStreamerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WeatherStreamerDbContext>();
        // Use SQLite with a local file for design-time operations
        optionsBuilder.UseSqlite("Data Source=weather-streamer.db");
        return new WeatherStreamerDbContext(optionsBuilder.Options);
    }
}
