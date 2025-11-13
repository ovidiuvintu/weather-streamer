using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

public class UpdateSimulationAuditPersistenceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UpdateSimulationAuditPersistenceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "UpdateSimAuditDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Patch_Persists_AuditEntry_WithChanges_AndActor()
    {
        // Arrange: seed simulation
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation
        {
            Name = "Original",
            StartTime = DateTime.UtcNow.AddDays(1),
            FileName = "data.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

        var currentEtag = Convert.ToBase64String(entity.RowVersion!);

        // Act: PATCH name only with If-Match
        var payload = new { name = "UpdatedNameForAudit" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{entity.Id}") { Content = content };
        request.Headers.TryAddWithoutValidation("If-Match", currentEtag);

        var resp = await _client.SendAsync(request);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: audit entry persisted
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();

        var audit = await db2.AuditEntries
            .FirstOrDefaultAsync(a => a.SimulationId == entity.Id && a.ChangesJson.Contains("UpdatedNameForAudit"));

        audit.Should().NotBeNull();
        audit!.SimulationId.Should().Be(entity.Id);
        // Controller currently records actor as 'anonymous' when no auth present
        audit.Actor.Should().Be("anonymous");
        audit.ChangesJson.Should().Contain("Name");
    }
}
