using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Validators;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Application.Services.Simulations.Update;

/// <summary>
/// Handles UpdateSimulationCommand with optimistic concurrency.
/// </summary>
public class UpdateSimulationHandler
{
    private readonly ISimulationRepository _repository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UpdateSimulationHandler> _logger;

    public UpdateSimulationHandler(ISimulationRepository repository, IAuditRepository auditRepository, ILogger<UpdateSimulationHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SimulationListItem?> HandleAsync(UpdateSimulationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Id <= 0) throw new ArgumentOutOfRangeException(nameof(command.Id));
        if (string.IsNullOrWhiteSpace(command.IfMatch)) throw new ArgumentException("If-Match is required.", nameof(command.IfMatch));

        var entity = await _repository.GetByIdTrackedAsync(command.Id, cancellationToken);
        if (entity is null)
        {
            return null; // not found
        }

        // Parse potential StartTime early so we can perform domain immutability checks before applying changes
        DateTime? parsedStartTimeUtc = null;
        if (command.StartTime is not null)
        {
            if (DateTime.TryParse(command.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                parsedStartTimeUtc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            }
        }

        // Domain immutability checks: prevent changing StartTime/FileName if simulation already started
        try
        {
            entity.EnsureMutableForUpdate(command.DataSource, parsedStartTimeUtc);
        }
        catch (InvalidOperationException ex)
        {
            // Surface as argument exception so controller maps to 400 Bad Request
            throw new ArgumentException(ex.Message, ex);
        }

        // Apply partial updates (Name, StartTime, DataSource, Status)
        if (command.Name is not null)
        {
            entity.Name = command.Name;
        }

        if (parsedStartTimeUtc.HasValue)
        {
            entity.StartTime = parsedStartTimeUtc.Value;
        }

        if (command.DataSource is not null)
        {
            // Additional format validation for DataSource should only apply when changing and when simulation is NotStarted
            if (!UpdateSimulationCommandValidator.IsValidDataSource(command.DataSource))
            {
                throw new ArgumentException("DataSource is invalid or contains unsupported characters.", nameof(command.DataSource));
            }

            entity.FileName = command.DataSource;
        }

        if (command.Status is not null)
        {
            if (Enum.TryParse<SimulationStatus>(command.Status.Replace(" ", string.Empty), ignoreCase: true, out var status))
            {
                entity.Status = status;
            }
        }

        // Snapshot before update for audit
        var before = new
        {
            Id = entity.Id,
            Name = entity.Name,
            StartTime = entity.StartTime,
            FileName = entity.FileName,
            Status = entity.Status,
            RowVersion = entity.RowVersion is null ? null : Convert.ToBase64String(entity.RowVersion)
        };

        // Decode If-Match
        byte[] ifMatchBytes;
        try
        {
            ifMatchBytes = Convert.FromBase64String(command.IfMatch);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid If-Match token; must be base64.", nameof(command.IfMatch));
        }

        var updated = await _repository.UpdateAsync(entity, ifMatchBytes, cancellationToken);

        // Build change set for audit logging
        var changes = new List<object>();
        if (!string.Equals(before.Name, updated.Name, StringComparison.Ordinal))
            changes.Add(new { field = "Name", before = before.Name, after = updated.Name });
        if (before.StartTime != updated.StartTime)
            changes.Add(new { field = "StartTime", before = before.StartTime, after = updated.StartTime });
        if (!string.Equals(before.FileName, updated.FileName, StringComparison.Ordinal))
            changes.Add(new { field = "FileName", before = before.FileName, after = updated.FileName });
        if (!before.Status.Equals(updated.Status))
            changes.Add(new { field = "Status", before = before.Status.ToString(), after = updated.Status.ToString() });

        var prevEtag = before.RowVersion;
        var newEtag = updated.RowVersion is null ? null : Convert.ToBase64String(updated.RowVersion);

        // Log audit entry including actor and correlation id if provided
        var actorToLog = command.Actor ?? "anonymous";
        var corr = command.CorrelationId ?? ResponseCorrelationIdFallback();
        try
        {
            _logger.LogInformation("Audit: Actor={Actor} CorrelationId={CorrelationId} SimulationId={Id} Changes={Changes} PrevETag={PrevETag} NewETag={NewETag}",
                actorToLog,
                corr,
                updated.Id,
                JsonSerializer.Serialize(changes),
                prevEtag,
                newEtag);

            // Persist audit entry
            var audit = new WeatherStreamer.Domain.Entities.AuditEntry
            {
                SimulationId = updated.Id,
                Actor = actorToLog,
                CorrelationId = corr,
                TimestampUtc = DateTime.UtcNow,
                ChangesJson = JsonSerializer.Serialize(changes),
                PrevETag = prevEtag,
                NewETag = newEtag
            };

            await _auditRepository.CreateAsync(audit, cancellationToken);
        }
        catch
        {
            // Audit persistence/logging should not block the update; swallow any exceptions
        }

        // Helper to provide a sensible fallback when correlation id not supplied
        static string? ResponseCorrelationIdFallback()
        {
            return null;
        }
        return new SimulationListItem
        {
            Id = updated.Id,
            Name = updated.Name,
            StartTimeUtc = updated.StartTime.Kind == DateTimeKind.Utc ? updated.StartTime : updated.StartTime.ToUniversalTime(),
            FileName = updated.FileName,
            Status = updated.Status.ToString(),
            ETag = updated.RowVersion is null ? null : Convert.ToBase64String(updated.RowVersion)
        };
    }
}
