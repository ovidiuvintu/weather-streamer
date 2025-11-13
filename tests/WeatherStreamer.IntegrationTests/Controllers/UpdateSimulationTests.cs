using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherStreamer.Api.Models;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Data;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class UpdateSimulationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UpdateSimulationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "UpdateSimDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Patch_NameOnly_Update_Returns200_WithNewEtag()
    {
        // Arrange: seed an entity with a rowversion so GET exposes ETag
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation
        {
            Name = "Original",
            StartTime = new DateTime(2026, 01, 01, 10, 00, 00, DateTimeKind.Utc),
            FileName = "data.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

    // Compute current ETag from seeded rowversion (InMemory provider may not return ETag via GET reliably)
    var currentEtag = Convert.ToBase64String(entity.RowVersion!);

        // Act: PATCH name only with If-Match
        var payload = new { name = "Updated" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{entity.Id}");
        request.Content = content;
        request.Headers.TryAddWithoutValidation("If-Match", currentEtag);

        var patchResp = await _client.SendAsync(request);

        // Assert
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        // Expect a new ETag header to be returned after successful update
        patchResp.Headers.ETag.Should().NotBeNull();
        var newEtag = patchResp.Headers.ETag!.Tag.Trim('"');
        newEtag.Should().NotBeNullOrEmpty();
        // Note: On some providers InMemory may not auto-change rowversion; handler should ensure a new token
        newEtag.Should().NotBe(currentEtag);

        var dto = await patchResp.Content.ReadFromJsonAsync<SimulationDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(entity.Id);
        dto.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Patch_WithStale_IfMatch_Returns409()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation
        {
            Name = "Original",
            StartTime = new DateTime(2026, 01, 01, 10, 00, 00, DateTimeKind.Utc),
            FileName = "data.csv",
            Status = SimulationStatus.NotStarted,
            RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }
        };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

        // Use a bogus/stale ETag (does not match current)
        var staleEtag = Convert.ToBase64String(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        var payload = new { name = "Updated" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{entity.Id}");
        request.Content = content;
        request.Headers.TryAddWithoutValidation("If-Match", staleEtag);

        // Act
        var patchResp = await _client.SendAsync(request);

        // Assert
        patchResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Patch_ChangingDataSource_OnStartedSimulation_Returns400()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamerDbContext>();
        var entity = new Simulation
        {
            Name = "Original",
            StartTime = new DateTime(2026, 01, 01, 10, 00, 00, DateTimeKind.Utc),
            FileName = "data.csv",
            Status = SimulationStatus.InProgress,
            RowVersion = new byte[] { 4, 4, 4, 4, 4, 4, 4, 4 }
        };
        db.Simulations.Add(entity);
        await db.SaveChangesAsync();

        var currentEtag = Convert.ToBase64String(entity.RowVersion!);

        // Act: attempt to change DataSource on a started simulation
        var payload = new { dataSource = "newdata.csv" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{entity.Id}");
        request.Content = content;
        request.Headers.TryAddWithoutValidation("If-Match", currentEtag);

        var patchResp = await _client.SendAsync(request);

        // Assert
        patchResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await patchResp.Content.ReadFromJsonAsync<WeatherStreamer.Api.Models.ErrorResponse>();
        err.Should().NotBeNull();
            var details = err!.Details;
            details!.Should().ContainKey("payload");
            details!.TryGetValue("payload", out var payloadList).Should().BeTrue();
            payloadList.Should().NotBeNull();
            payloadList!.Any(m => m.Contains("Cannot change DataSource", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}
