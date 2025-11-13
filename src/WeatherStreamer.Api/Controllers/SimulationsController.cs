using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using WeatherStreamer.Api.Models;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Services;
using WeatherStreamer.Application.Services.Simulations;
using WeatherStreamer.Application.Services.Simulations.Update;

namespace WeatherStreamer.Api.Controllers;

/// <summary>
/// Controller for simulation management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SimulationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;
    private readonly ISimulationReadService _readService;
    private readonly IValidator<CreateSimulationRequest> _validator;
    private readonly UpdateSimulationHandler _updateHandler;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        ISimulationService simulationService,
        ISimulationReadService readService,
        IValidator<CreateSimulationRequest> validator,
        UpdateSimulationHandler updateHandler,
        ILogger<SimulationsController> logger)
    {
        _simulationService = simulationService ?? throw new ArgumentNullException(nameof(simulationService));
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _updateHandler = updateHandler ?? throw new ArgumentNullException(nameof(updateHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all simulations ordered by StartTime then Id.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SimulationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSimulations(CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        var items = await _readService.GetAllAsync(cancellationToken);
        var dtos = items.Select(MapToApiDto).ToList();
        _logger.LogInformation("GET /api/simulations returned {Count} items in {Ms} ms", dtos.Count, (DateTime.UtcNow - start).TotalMilliseconds);
        return Ok(dtos);
    }

    /// <summary>
    /// Partially updates a simulation using optimistic concurrency via If-Match header.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(SimulationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSimulation([FromRoute] int id, [FromBody] UpdateSimulationRequest request, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Invalid id",
                Details = new Dictionary<string, List<string>>
                {
                    { "id", new List<string> { "Id must be a positive integer." } }
                }
            });
        }

        var ifMatch = Request.Headers["If-Match"].ToString();
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Missing If-Match header",
                Details = new Dictionary<string, List<string>>
                {
                    { "If-Match", new List<string> { "The If-Match header is required for concurrency control." } }
                }
            });
        }

            try
            {
                // Ensure a correlation id is present for tracing
                var correlationId = Request.Headers["X-Correlation-ID"].ToString();
                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                    Response.Headers["X-Correlation-ID"] = correlationId;
                }

                // Actor: prefer authenticated user name, otherwise anonymous
                var actor = HttpContext?.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(actor)) actor = "anonymous";

                var cmd = new UpdateSimulationCommand
                {
                    Id = id,
                    Name = request.Name,
                    StartTime = request.StartTime,
                    DataSource = request.DataSource,
                    Status = request.Status,
                    IfMatch = ifMatch,
                    Actor = actor,
                    CorrelationId = correlationId
                };

            var updated = await _updateHandler.HandleAsync(cmd, cancellationToken);
            if (updated is null)
            {
                return NotFound(new ErrorResponse
                {
                    CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                    Timestamp = DateTime.UtcNow,
                    StatusCode = StatusCodes.Status404NotFound,
                    Error = "Not Found",
                    Details = new Dictionary<string, List<string>>
                    {
                        { "id", new List<string> { $"Simulation with id {id} was not found." } }
                    }
                });
            }

            if (!string.IsNullOrEmpty(updated.ETag))
            {
                // Wrap ETag in quotes per RFC (many clients expect quoted value)
                Response.Headers.ETag = '"' + updated.ETag + '"';
            }

            var dto = MapToApiDto(updated);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Validation failed",
                Details = new Dictionary<string, List<string>>
                {
                    { ex.ParamName ?? "payload", new List<string> { ex.Message } }
                }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Concurrency conflict", StringComparison.OrdinalIgnoreCase))
        {
            // Attempt to include the current resource version in the response for client to retry
            var current = await _readService.GetByIdAsync(id, cancellationToken: cancellationToken);
            var currentVersion = current?.ETag;
            var details = new Dictionary<string, List<string>>
            {
                { "If-Match", new List<string> { "The provided version does not match the current resource version." } }
            };
            if (!string.IsNullOrEmpty(currentVersion))
            {
                details.Add("currentVersion", new List<string> { currentVersion });
            }

            return Conflict(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status409Conflict,
                Error = "Concurrency conflict",
                Details = details
            });
        }
    }
    /// <summary>
    /// Retrieves simulations with StartTime greater than or equal to the provided UTC boundary.
    /// </summary>
    /// <param name="start_time">ISO-8601 timestamp string; treated as UTC. Example: 2025-01-15T10:30:00Z</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("by-start-time")]
    [ProducesResponseType(typeof(IEnumerable<SimulationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSimulationsFromStartTime([FromQuery(Name = "start_time")] string? start_time, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(start_time))
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Invalid start_time",
                Details = new Dictionary<string, List<string>>
                {
                    { "start_time", new List<string> { "The 'start_time' query parameter is required and must be a valid ISO-8601 timestamp." } }
                }
            });
        }

        if (!DateTimeOffset.TryParse(start_time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Invalid start_time",
                Details = new Dictionary<string, List<string>>
                {
                    { "start_time", new List<string> { "The 'start_time' must be a valid ISO-8601 timestamp (e.g., 2025-01-15T10:30:00Z)." } }
                }
            });
        }

        var boundaryUtc = dto.UtcDateTime;
        var t0 = DateTime.UtcNow;
        var items = await _readService.GetFromStartTimeAsync(boundaryUtc, cancellationToken);
        var dtos = items.Select(MapToApiDto).ToList();
        _logger.LogInformation("GET /api/simulations/by-start-time?start_time={Boundary} returned {Count} items in {Ms} ms", start_time, dtos.Count, (DateTime.UtcNow - t0).TotalMilliseconds);
        return Ok(dtos);
    }

    /// <summary>
    /// Retrieves a single simulation by id.
    /// </summary>
    /// <param name="id">Simulation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SimulationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSimulationById([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status400BadRequest,
                Error = "Invalid id",
                Details = new Dictionary<string, List<string>>
                {
                    { "id", new List<string> { "Id must be a positive integer." } }
                }
            });
        }

        var start = DateTime.UtcNow;
        var item = await _readService.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            _logger.LogInformation("GET /api/simulations/{Id} not found in {Ms} ms", id, (DateTime.UtcNow - start).TotalMilliseconds);
            return NotFound(new ErrorResponse
            {
                CorrelationId = Response.Headers["X-Correlation-ID"].ToString(),
                Timestamp = DateTime.UtcNow,
                StatusCode = StatusCodes.Status404NotFound,
                Error = "Not Found",
                Details = new Dictionary<string, List<string>>
                {
                    { "id", new List<string> { $"Simulation with id {id} was not found." } }
                }
            });
        }

        _logger.LogInformation("GET /api/simulations/{Id} returned in {Ms} ms", id, (DateTime.UtcNow - start).TotalMilliseconds);
        if (!string.IsNullOrEmpty(item.ETag))
        {
            Response.Headers.ETag = item.ETag;
        }
        return Ok(MapToApiDto(item));
    }

    private static SimulationDto MapToApiDto(SimulationListItem s)
        => new()
        {
            Id = s.Id,
            Name = s.Name,
            StartTime = s.StartTimeUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            FileName = s.FileName,
            Status = s.Status
        };

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

            // Add Location header to canonical resource URL
            var location = $"/api/simulations/{simulationId}";
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
