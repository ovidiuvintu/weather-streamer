using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly WeatherStreamerDbContext _context;

    public AuditRepository(WeatherStreamerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuditEntry> CreateAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default)
    {
        _context.Add(auditEntry);
        await _context.SaveChangesAsync(cancellationToken);
        return auditEntry;
    }
}
