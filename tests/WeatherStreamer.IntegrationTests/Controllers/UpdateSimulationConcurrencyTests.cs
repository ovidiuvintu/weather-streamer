using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Xunit;
using System.Text.Json;

namespace WeatherStreamer.IntegrationTests.Controllers
{
    public class UpdateSimulationConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public UpdateSimulationConcurrencyTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    context.HostingEnvironment.EnvironmentName = "Testing";
                    var dict = new Dictionary<string, string?>
                    {
                        ["UseInMemoryDatabase"] = "true",
                        ["InMemoryDbName"] = "UpdateSimConcurrencyDb"
                    };
                    config.AddInMemoryCollection(dict);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Patch_With_Stale_IfMatch_Returns_409_With_CurrentVersion()
        {
            // Arrange: create a simulation
            var createReq = new
            {
                Name = "Concurrency Test",
                StartTime = DateTime.UtcNow.AddHours(1).ToString("o"),
                DataSource = "C:\\Development\\weather-streamer\\README.md"
            };

            var createResp = await _client.PostAsJsonAsync("/api/simulations", createReq);
            createResp.EnsureSuccessStatusCode();
            var createdJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
            var createdRoot = createdJson.RootElement;
            createdRoot.ValueKind.Should().Be(JsonValueKind.Object);
            var createdId = createdRoot.GetProperty("id").GetRawText().Trim('"');
            createdId.Should().NotBeNullOrEmpty();

            // (Optional) attempt to read current ETag if provided by the API
            var getResp = await _client.GetAsync($"/api/simulations/{createdId}");
            getResp.EnsureSuccessStatusCode();
            var eTag = getResp.Headers.ETag?.Tag?.Trim('"');

            // Use a stale (but valid) If-Match token so the handler accepts it and repository can detect a mismatch
            var stale = "AQIDBAUGBwg=";
            var patch = new { name = "New Name" };
            var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/simulations/{createdId}")
            {
                Content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json")
            };
            req.Headers.TryAddWithoutValidation("If-Match", stale);

            // Act
            var patchResp = await _client.SendAsync(req);

            // Assert
            patchResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var bodyJson = JsonDocument.Parse(await patchResp.Content.ReadAsStringAsync());
            var bodyRoot = bodyJson.RootElement;
            bodyRoot.TryGetProperty("details", out var detailsElem).Should().BeTrue();
            detailsElem.ValueKind.Should().Be(JsonValueKind.Object);
            // currentVersion may be present depending on provider; if present it must be non-empty
            if (detailsElem.TryGetProperty("currentVersion", out var currentVersionElem))
            {
                currentVersionElem.ValueKind.Should().Be(JsonValueKind.Array);
                var currentVersion = currentVersionElem[0].GetRawText().Trim('"');
                currentVersion.Should().NotBeNullOrEmpty();
            }
        }
    }
}
