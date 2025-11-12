using Microsoft.Extensions.Logging;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Repositories;

namespace WeatherStreamer.Application.Services.Simulations;

/// <summary>
/// Read-only service implementation for simulations.
/// </summary>
public class SimulationReadService : ISimulationReadService
{
    private readonly ISimulationReadRepository _repository;
    private readonly ILogger<SimulationReadService> _logger;

    public SimulationReadService(ISimulationReadRepository repository, ILogger<SimulationReadService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<SimulationListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;
        var entities = await _repository.GetAllAsync(cancellationToken);
        var items = entities
            .Select(MapToDto)
            .OrderBy(x => x.StartTimeUtc)
            .ThenBy(x => x.Id)
            .ToList();
        _logger.LogInformation("Retrieved {Count} simulations in {DurationMs} ms", items.Count, (DateTime.UtcNow - start).TotalMilliseconds);
        return items;
    }

    public async Task<IReadOnlyList<SimulationListItem>> GetFromStartTimeAsync(DateTime boundaryUtc, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;
        var entities = await _repository.GetFromStartTimeAsync(boundaryUtc, cancellationToken);
        var items = entities
            .Select(MapToDto)
            .OrderBy(x => x.StartTimeUtc)
            .ThenBy(x => x.Id)
            .ToList();
        _logger.LogInformation("Retrieved {Count} simulations from {Boundary} in {DurationMs} ms", items.Count, boundaryUtc, (DateTime.UtcNow - start).TotalMilliseconds);
        return items;
    }

    public async Task<SimulationListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be a positive integer.");
        }

        var start = DateTime.UtcNow;
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        var item = entity is null ? null : MapToDto(entity);
        _logger.LogInformation("Retrieved by id {Id}: {Found} in {DurationMs} ms", id, item != null, (DateTime.UtcNow - start).TotalMilliseconds);
        return item;
    }

    private static SimulationListItem MapToDto(WeatherStreamer.Domain.Entities.Simulation s)
        => new()
        {
            Id = s.Id,
            Name = s.Name,
            StartTimeUtc = s.StartTime.Kind == DateTimeKind.Utc ? s.StartTime : s.StartTime.ToUniversalTime(),
            FileName = s.FileName,
            Status = s.Status.ToString()
        };
}
