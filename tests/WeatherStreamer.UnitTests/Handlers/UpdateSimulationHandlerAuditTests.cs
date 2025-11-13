using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Services.Simulations.Update;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using Xunit;

namespace WeatherStreamer.UnitTests.Handlers;

public class UpdateSimulationHandlerAuditTests
{
    [Fact]
    public async Task HandleAsync_WhenNameChanged_PersistsAuditEntry()
    {
        // Arrange
        var simulation = new Simulation
        {
            Id = 10,
            Name = "Before",
            StartTime = DateTime.UtcNow.AddDays(1),
            FileName = "file.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        var repo = new FakeSimulationRepository(simulation);
        var auditStore = new InMemoryAuditRepository();
        var handler = new UpdateSimulationHandler(repo, auditStore, new NullLogger<UpdateSimulationHandler>());

        var cmd = new UpdateSimulationCommand
        {
            Id = simulation.Id,
            Name = "After",
            IfMatch = Convert.ToBase64String(simulation.RowVersion!)
        };

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        auditStore.Saved.Should().HaveCount(1);
        var audit = auditStore.Saved[0];
        audit.SimulationId.Should().Be(simulation.Id);
        audit.ChangesJson.Should().Contain("Before");
        audit.ChangesJson.Should().Contain("After");
    }

    class InMemoryAuditRepository : IAuditRepository
    {
        public List<AuditEntry> Saved { get; } = new();

        public Task<AuditEntry> CreateAsync(AuditEntry auditEntry, CancellationToken cancellationToken = default)
        {
            Saved.Add(auditEntry);
            return Task.FromResult(auditEntry);
        }
    }

    class FakeSimulationRepository : ISimulationRepository
    {
        private Simulation _entity;
        public FakeSimulationRepository(Simulation entity) => _entity = entity;

        public Task<Simulation> CreateAsync(Simulation simulation, CancellationToken cancellationToken = default)
        {
            simulation.Id = _entity.Id; // keep the id stable for test
            _entity = simulation;
            return Task.FromResult(simulation);
        }

        public Task<bool> IsFileInUseAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Simulation?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Simulation?>(_entity.Id == id ? _entity : null);
        }

        public Task<Simulation> UpdateAsync(Simulation entity, byte[] ifMatch, CancellationToken cancellationToken = default)
        {
            // Simple concurrency check
            if (_entity.RowVersion is null || ifMatch is null || _entity.RowVersion.Length != ifMatch.Length)
                throw new InvalidOperationException("Concurrency conflict");

            for (int i = 0; i < _entity.RowVersion.Length; i++)
            {
                if (_entity.RowVersion[i] != ifMatch[i])
                    throw new InvalidOperationException("Concurrency conflict");
            }

            // Apply updates
            _entity.Name = entity.Name;
            // bump rowversion synthetic
            _entity.RowVersion = new byte[] { 9, 9, 9, 9 };
            return Task.FromResult(_entity);
        }

        // The rest of the repo interface is not needed for this test; provide throw stubs
        public Task<List<Simulation>> ListAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> AddAsync(Simulation entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Simulation?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
