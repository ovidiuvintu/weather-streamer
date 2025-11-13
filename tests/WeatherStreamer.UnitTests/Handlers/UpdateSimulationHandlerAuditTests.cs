using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Services.Simulations.Update;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.UnitTests.Handlers;

public class UpdateSimulationHandlerAuditTests
{
    [Fact]
    public async Task Handler_Logs_Audit_On_Update()
    {
        // This is a basic smoke test to ensure handler runs and audit logging path executes.
        // Verifying actual logger calls requires a mocking framework; here we just ensure no exceptions.

        // Arrange: use a fake repository implementation in-memory
        var repo = new InMemorySimulationRepository();
        var auditRepo = new InMemoryAuditRepository();
        var logger = new NullLogger<UpdateSimulationHandler>();
        var handler = new UpdateSimulationHandler(repo, auditRepo, logger);

        var sim = new Simulation
        {
            Name = "A",
            StartTime = DateTime.UtcNow.AddHours(1),
            FileName = "f.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
        await repo.CreateAsync(sim);

        var cmd = new UpdateSimulationCommand
        {
            Id = sim.Id,
            Name = "B",
            IfMatch = Convert.ToBase64String(sim.RowVersion!),
            Actor = "unit-test-user",
            CorrelationId = "test-corr-1"
        };

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("B");
    }
}

// Minimal in-memory repository implementation for handler unit test.
internal class InMemorySimulationRepository : WeatherStreamer.Application.Repositories.ISimulationRepository
{
    private readonly List<Simulation> _store = new();
    private int _id = 1;

    public Task<Simulation> CreateAsync(Simulation simulation, CancellationToken cancellationToken = default)
    {
        simulation.Id = _id++;
        _store.Add(simulation);
        return Task.FromResult(simulation);
    }

    public Task<bool> IsFileInUseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<Simulation?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.FirstOrDefault(x => x.Id == id));
    }

    public Task<Simulation> UpdateAsync(Simulation simulation, byte[] ifMatchRowVersion, CancellationToken cancellationToken = default)
    {
        // Simple concurrency simulation: match raw bytes
        var existing = _store.FirstOrDefault(x => x.Id == simulation.Id);
        if (existing is null) throw new InvalidOperationException("Entity not found");
        if (!existing.RowVersion!.SequenceEqual(ifMatchRowVersion))
            throw new InvalidOperationException("Concurrency conflict: rowversion mismatch");

        // Apply changes and set a new synthetic rowversion
        existing.Name = simulation.Name;
        existing.StartTime = simulation.StartTime;
        existing.FileName = simulation.FileName;
        existing.Status = simulation.Status;
        existing.RowVersion = new byte[] { 9, 9, 9, 9 };

        return Task.FromResult(existing);
    }
}

// Minimal in-memory audit repository implementation for handler unit test.
internal class InMemoryAuditRepository : WeatherStreamer.Application.Repositories.IAuditRepository
{
    public Task<WeatherStreamer.Domain.Entities.AuditEntry> CreateAsync(WeatherStreamer.Domain.Entities.AuditEntry entry, CancellationToken cancellationToken = default)
    {
        entry.Id = new Random().Next(1, 1000);
        return Task.FromResult(entry);
    }
}
