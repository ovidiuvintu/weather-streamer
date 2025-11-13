using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class UpdateSimulationIllegalTransitionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UpdateSimulationIllegalTransitionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "UpdateSimIllegalTransitionDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Patch_NotStarted_To_Completed_Returns_400_With_Message()
    {
        // Arrange: seed a simulation in NotStarted with a rowversion so we have a stable ETag
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WeatherStreamer.Infrastructure.Data.WeatherStreamerDbContext>();
        var seeded = new WeatherStreamer.Domain.Entities.Simulation
        {
            Name = "Transition Test",
            StartTime = DateTime.UtcNow.AddHours(1),
            FileName = "README.md",
            Status = WeatherStreamer.Domain.Enums.SimulationStatus.NotStarted,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        };
        db.Simulations.Add(seeded);
        await db.SaveChangesAsync();
        var createdId = seeded.Id.ToString();
        var eTag = Convert.ToBase64String(seeded.RowVersion!);

        // Attempt illegal transition NotStarted -> Completed
        var patch = new { status = "Completed" };
        var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{createdId}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("If-Match", eTag ?? "");

        // Act
        var patchResp = await _client.SendAsync(req);

        // Assert
        patchResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await patchResp.Content.ReadAsStringAsync();
        var bodyJson = JsonDocument.Parse(raw);
        var root = bodyJson.RootElement;
        root.TryGetProperty("details", out var detailsElem).Should().BeTrue();
        detailsElem.ValueKind.Should().Be(JsonValueKind.Object);
        detailsElem.TryGetProperty("status", out var statusElem).Should().BeTrue();
        statusElem.ValueKind.Should().Be(JsonValueKind.Array);
        var msg = statusElem[0].GetString();
        msg.Should().Contain("Illegal status transition");
    }
}
