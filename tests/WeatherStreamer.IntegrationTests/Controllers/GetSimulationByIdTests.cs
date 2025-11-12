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

public class GetSimulationByIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetSimulationByIdTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "GetByIdDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ById_Found_Returns200()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation { Name = "One", StartTime = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc), FileName = "one.csv", Status = SimulationStatus.NotStarted };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/simulations/{entity.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<SimulationDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
    }

    [Fact]
    public async Task Get_ById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/simulations/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Get_ById_InvalidId_Returns400(int id)
    {
        var response = await _client.GetAsync($"/api/simulations/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
