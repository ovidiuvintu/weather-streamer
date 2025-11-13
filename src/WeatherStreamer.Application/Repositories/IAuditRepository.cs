using WeatherStreamer.Domain.Entities;

namespace WeatherStreamer.Application.Repositories;

public interface IAuditRepository
{
    Task<AuditEntry> CreateAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default);
}
