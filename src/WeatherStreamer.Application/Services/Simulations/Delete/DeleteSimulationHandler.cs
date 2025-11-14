using Microsoft.Extensions.Logging;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.DTOs;

namespace WeatherStreamer.Application.Services.Simulations.Delete;

public class DeleteSimulationHandler
{
    private readonly ISimulationRepository _repository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<DeleteSimulationHandler> _logger;

    public DeleteSimulationHandler(ISimulationRepository repository, IAuditRepository auditRepository, ILogger<DeleteSimulationHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HandleAsync(DeleteSimulationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Id <= 0) throw new ArgumentOutOfRangeException(nameof(command.Id));
        if (string.IsNullOrWhiteSpace(command.IfMatch)) throw new ArgumentException("If-Match is required.", nameof(command.IfMatch));

        byte[] ifMatchBytes;
        try
        {
            ifMatchBytes = Convert.FromBase64String(command.IfMatch);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid If-Match token; must be base64.", nameof(command.IfMatch));
        }

        // Attempt delete
        var deleted = await _repository.SoftDeleteAsync(command.Id, ifMatchBytes, cancellationToken);
        if (!deleted)
            return false;

        // Create an audit entry for deletion. Attempt to include PrevETag when possible.
        try
        {
            // Attempt to read current entity to capture PrevETag and previous state
            // Note: repository does not expose a read method here; we rely on audit consumers
            var audit = new WeatherStreamer.Domain.Entities.AuditEntry
            {
                SimulationId = command.Id,
                Actor = command.Actor ?? "anonymous",
                CorrelationId = command.CorrelationId,
                TimestampUtc = DateTime.UtcNow,
                Action = "Delete",
                ChangesJson = "[ { \"field\": \"IsDeleted\", \"before\": false, \"after\": true } ]",
                PrevETag = null,
                NewETag = null
            };

            await _auditRepository.CreateAsync(audit, cancellationToken);
        }
        catch
        {
            // Audit persistence should not block the delete operation
        }

        return true;
    }
}
