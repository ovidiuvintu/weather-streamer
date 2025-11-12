using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class SimulationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string TestDataRoot = "C:/test-data"; // Use forward slashes for consistency

    public SimulationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                context.HostingEnvironment.EnvironmentName = "Testing";
                var dict = new Dictionary<string, string?>
                {
                    ["UseInMemoryDatabase"] = "true",
                    ["InMemoryDbName"] = "SimulationsControllerTestsDb"
                };
                config.AddInMemoryCollection(dict);
            });
        });
        _client = _factory.CreateClient();
        EnsureDirectory();
    }

    private static void EnsureDirectory()
    {
        Directory.CreateDirectory(TestDataRoot);
    }

    private static void EnsureTestFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir!);
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "timestamp,value\n" + DateTime.UtcNow.ToString("O") + ",42" );
        }
    }

    [Fact]
    public async Task POST_Simulations_WithValidRequest_Returns201Created()
    {
        var request = new CreateSimulationRequest
        {
            Name = "Integration Test Simulation",
            StartTime = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            DataSource = @"C:\test-data\sample.csv"
        };
        EnsureTestFile(request.DataSource);

        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateSimulationResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be(request.Name);
        result.DataSource.Should().Be(request.DataSource);
        result.Status.Should().Be("NotStarted");
    }

    [Fact]
    public async Task POST_Simulations_VerifyLocationHeaderIn201Response()
    {
        var request = new CreateSimulationRequest
        {
            Name = "Location Header Test",
            StartTime = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            DataSource = @"C:\test-data\location-test.csv"
        };
        EnsureTestFile(request.DataSource);
        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().MatchRegex(@"/api/simulations/\d+");
    }

    [Fact]
    public async Task POST_Simulations_VerifyCorrelationIdInResponse()
    {
        var request = new CreateSimulationRequest
        {
            Name = "Correlation ID Test",
            StartTime = DateTime.UtcNow.AddHours(4).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            DataSource = @"C:\test-data\correlation-test.csv"
        };
        EnsureTestFile(request.DataSource);
        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.Headers.Should().ContainKey("X-Correlation-ID");
    }

    [Fact]
    public async Task POST_Simulations_PersistsToDatabase()
    {
        var request = new CreateSimulationRequest
        {
            Name = "Persistence Test",
            StartTime = DateTime.UtcNow.AddHours(5).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            DataSource = @"C:\test-data\persistence.csv"
        };
        EnsureTestFile(request.DataSource);
        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_Simulations_WithMissingName_Returns400BadRequest()
    {
        var request = new CreateSimulationRequest
        {
            Name = "",
            StartTime = DateTime.UtcNow.AddHours(6).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            DataSource = @"C:\test-data\invalid.csv"
        };
        EnsureTestFile(request.DataSource);
        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Simulations_WithMalformedJson_Returns400BadRequest()
    {
        var malformedJson = "{ \"Name\": \"X\", \"StartTime\": }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/simulations", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
