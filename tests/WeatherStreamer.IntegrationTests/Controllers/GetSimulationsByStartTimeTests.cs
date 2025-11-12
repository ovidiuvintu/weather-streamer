using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherStreamer.Api.Models;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class GetSimulationsByStartTimeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetSimulationsByStartTimeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "GetByStartTimeDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_FromStartTime_FiltersInclusively()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        db.Simulations.AddRange(
            new Simulation { Name = "Earlier", StartTime = new DateTime(2025, 12, 1, 9, 0, 0, DateTimeKind.Utc), FileName = "e.csv", Status = SimulationStatus.NotStarted },
            new Simulation { Name = "Boundary", StartTime = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc), FileName = "b.csv", Status = SimulationStatus.InProgress },
            new Simulation { Name = "Later", StartTime = new DateTime(2025, 12, 1, 11, 0, 0, DateTimeKind.Utc), FileName = "l.csv", Status = SimulationStatus.Completed }
        );
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/simulations/by-start-time?start_time=2025-12-01T10:00:00Z");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<SimulationDto>>();
        list.Should().NotBeNull();
        list!.Select(x => x.Name).Should().Equal("Boundary", "Later");
    }

    [Fact]
    public async Task Get_FromStartTime_WithNoMatches_ReturnsEmpty()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        db.Simulations.RemoveRange(db.Simulations);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/simulations/by-start-time?start_time=2030-01-01T00:00:00Z");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<SimulationDto>>();
        list.Should().NotBeNull();
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_FromStartTime_InvalidFormat_Returns400()
    {
        var response = await _client.GetAsync("/api/simulations/by-start-time?start_time=not-a-date");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
