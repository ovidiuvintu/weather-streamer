using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WeatherStreamer.Api.Models;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Services;

namespace WeatherStreamer.Api.Controllers;

/// <summary>
/// Controller for simulation management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SimulationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;
    private readonly IValidator<CreateSimulationRequest> _validator;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        ISimulationService simulationService,
        IValidator<CreateSimulationRequest> validator,
        ILogger<SimulationsController> logger)
    {
        _simulationService = simulationService ?? throw new ArgumentNullException(nameof(simulationService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new weather simulation.
    /// </summary>
    /// <param name="request">The simulation creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created simulation details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSimulation(
        [FromBody] CreateSimulationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var correlationId = Response.Headers["X-Correlation-ID"].ToString();
            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Validation failed",
                Details = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToList()
                    )
            };

            _logger.LogWarning("Validation failed for simulation creation: {Errors}", 
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            return BadRequest(errorResponse);
        }

        try
        {
            // Create simulation
            var simulationId = await _simulationService.CreateSimulationAsync(request, cancellationToken);

            // Parse start time for response
            DateTime.TryParse(request.StartTime, out var startTime);
            var startTimeUtc = startTime.Kind == DateTimeKind.Utc ? startTime : startTime.ToUniversalTime();

            var response = new CreateSimulationResponse
            {
                Id = simulationId,
                Name = request.Name,
                StartTimeUtc = startTimeUtc,
                DataSource = request.DataSource,
                Status = "NotStarted"
            };

            // Add Location header
            var location = Url.Action(nameof(CreateSimulation), new { id = simulationId }) ?? $"/api/simulations/{simulationId}";
            return Created(location, response);
        }
        catch (FileNotFoundException ex)
        {
            var correlationId = Response.Headers["X-Correlation-ID"].ToString();
            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status404NotFound,
                Error = "File not found",
                Details = new Dictionary<string, List<string>>
                {
                    { "DataSource", new List<string> { ex.Message } }
                }
            };

            _logger.LogWarning(ex, "File not found: {FilePath}", request.DataSource);
            return NotFound(errorResponse);
        }
        catch (DirectoryNotFoundException ex)
        {
            var correlationId = Response.Headers["X-Correlation-ID"].ToString();
            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Directory not found",
                Details = new Dictionary<string, List<string>>
                {
                    { "DataSource", new List<string> { ex.Message } }
                }
            };

            _logger.LogWarning(ex, "Directory not found for path: {FilePath}", request.DataSource);
            return BadRequest(errorResponse);
        }
        catch (IOException ex) when (ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase))
        {
            var correlationId = Response.Headers["X-Correlation-ID"].ToString();
            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status423Locked,
                Error = "File locked",
                Details = new Dictionary<string, List<string>>
                {
                    { "DataSource", new List<string> { ex.Message } }
                }
            };

            _logger.LogWarning(ex, "File locked: {FilePath}", request.DataSource);
            return StatusCode(StatusCodes.Status423Locked, errorResponse);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("in use", StringComparison.OrdinalIgnoreCase))
        {
            var correlationId = Response.Headers["X-Correlation-ID"].ToString();
            var errorResponse = new ErrorResponse
            {
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status409Conflict,
                Error = "Resource conflict",
                Details = new Dictionary<string, List<string>>
                {
                    { "DataSource", new List<string> { ex.Message } }
                }
            };

            _logger.LogWarning(ex, "File in use: {FilePath}", request.DataSource);
            return Conflict(errorResponse);
        }
    }
}
