using Microsoft.Extensions.Logging;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Application.Services;

/// <summary>
/// Service implementation for simulation business logic.
/// </summary>
public class SimulationService : ISimulationService
{
    private readonly ISimulationRepository _repository;
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(
        ISimulationRepository repository,
        IFileValidationService fileValidationService,
        ILogger<SimulationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> CreateSimulationAsync(CreateSimulationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating simulation: {Name}", request.Name);

        // Validate file exists and is accessible
        await _fileValidationService.ValidateFileAsync(request.DataSource, cancellationToken);

        // Check for concurrent file usage
        var isFileInUse = await _repository.IsFileInUseAsync(request.DataSource, cancellationToken);
        if (isFileInUse)
        {
            _logger.LogWarning("File {FilePath} is currently in use by another in-progress simulation", request.DataSource);
            throw new InvalidOperationException($"The file '{request.DataSource}' is currently in use by another simulation which is In Progress");
        }

        // Parse and convert start time to UTC
        if (!DateTime.TryParse(request.StartTime, out var startTime))
        {
            throw new ArgumentException($"Invalid StartTime format: {request.StartTime}. Expected ISO 8601 format.", nameof(request.StartTime));
        }

        var startTimeUtc = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();

        // Create simulation entity
        var simulation = new Simulation
        {
            Name = request.Name,
            StartTime = startTimeUtc,
            FileName = request.DataSource,
            Status = SimulationStatus.NotStarted
        };

        // Persist to database
        try
        {
            var created = await _repository.CreateAsync(simulation, cancellationToken);

            _logger.LogInformation("Successfully created simulation with ID: {SimulationId}", created.Id);

            return created.Id;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("database error", StringComparison.OrdinalIgnoreCase))
        {
            // Database error from repository - log detailed information
            _logger.LogError(ex, 
                "Database error while creating simulation. Name: {Name}, DataSource: {DataSource}, StartTime: {StartTime}. Error: {ErrorMessage}",
                request.Name, request.DataSource, startTimeUtc, ex.Message);
            
            // Re-throw to be handled by controller/middleware
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected error - log with full details
            _logger.LogError(ex, 
                "Unexpected error while creating simulation. Name: {Name}, DataSource: {DataSource}, Exception: {ExceptionType}",
                request.Name, request.DataSource, ex.GetType().Name);
            
            throw;
        }
    }
}
