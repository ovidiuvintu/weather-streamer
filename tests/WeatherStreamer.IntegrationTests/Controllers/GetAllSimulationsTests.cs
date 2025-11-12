using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WeatherStreamer.Api.Models;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class GetAllSimulationsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetAllSimulationsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "ListAllSimulations"
                };
                config.AddInMemoryCollection(dict);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsEmptyArray_WhenNoSimulations()
    {
        var response = await _client.GetAsync("/api/simulations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<SimulationDto>>();
        list.Should().NotBeNull();
        list!.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsOrderedList_WhenSimulationsExist()
    {
        // Seed
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        db.Simulations.AddRange(
            new Simulation { Name = "A", StartTime = new DateTime(2025,12,01,09,00,00, DateTimeKind.Utc), FileName = "a.csv", Status = SimulationStatus.NotStarted },
            new Simulation { Name = "B", StartTime = new DateTime(2025,12,01,10,00,00, DateTimeKind.Utc), FileName = "b.csv", Status = SimulationStatus.InProgress },
            new Simulation { Name = "C", StartTime = new DateTime(2025,12,01,10,00,01, DateTimeKind.Utc), FileName = "c.csv", Status = SimulationStatus.Completed }
        );
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/simulations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<SimulationDto>>();
        list.Should().NotBeNull();
        list!.Count.Should().Be(3);
        list.Select(s => s.Name).Should().Equal("A","B","C");
    }
}
