using AspNetCoreRateLimit;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WeatherStreamer.Api.Middleware;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Services;
using WeatherStreamer.Infrastructure.Data;
using WeatherStreamer.Infrastructure.Repositories;
using WeatherStreamer.Infrastructure.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/weather-streamer-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Weather Streamer API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add controllers with JSON options to reject additional properties
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Reject additional properties not in the model (US2 - T045)
            options.JsonSerializerOptions.AllowTrailingCommas = false;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            // Note: System.Text.Json doesn't have direct "DisallowUnmappedMembers" 
            // Additional property validation handled in controller via model binding errors
        });

    // Configure DbContext
    builder.Services.AddDbContext<WeatherStreamerDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlite(connectionString);
    });

    // Register repositories
    builder.Services.AddScoped<ISimulationRepository, SimulationRepository>();

    // Register services
    builder.Services.AddScoped<ISimulationService, WeatherStreamer.Application.Services.SimulationService>();
    builder.Services.AddScoped<IFileValidationService, FileValidationService>();

    // Add FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(WeatherStreamer.Application.Validators.CreateSimulationRequestValidator).Assembly);

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<WeatherStreamerDbContext>("database");

    // Configure rate limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Weather Streamer API",
            Version = "v1",
            Description = "API for managing weather simulation data streams"
        });
    });

    var app = builder.Build();

    // Configure middleware pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Streamer API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    // Rate limiting
    app.UseIpRateLimiting();

    app.UseAuthorization();

    app.MapControllers();
    
    // Map health check endpoint
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class accessible for integration tests
public partial class Program { }
