using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class DeleteSimulationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeleteSimulationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "DeleteSimDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Delete_WithValidIfMatch_Persists_AuditEntry_And_Returns_204()
    {
        // Arrange: seed simulation in NotStarted (allowed to delete per spec)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation
        {
            Name = "ToDelete",
            StartTime = DateTime.UtcNow.AddDays(1),
            FileName = "data.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 5, 6, 7, 8 }
        };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

        var currentEtag = Convert.ToBase64String(entity.RowVersion!);

        // Act: DELETE with If-Match header
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/simulations/{entity.Id}");
        request.Headers.TryAddWithoutValidation("If-Match", currentEtag);

        var resp = await _client.SendAsync(request);

        // Assert: 204 No Content
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: audit entry persisted with Action = "Delete"
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();

        var audit = await db2.AuditEntries
            .FirstOrDefaultAsync(a => a.SimulationId == entity.Id && a.Action == "Delete");

        audit.Should().NotBeNull();
        audit!.SimulationId.Should().Be(entity.Id);
        audit.Actor.Should().Be("anonymous");
        audit.Action.Should().Be("Delete");
    }
}
