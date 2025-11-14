using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Services;
using WeatherStreamer.Api.Models;

namespace WeatherStreamer.IntegrationTests.Controllers;

public class EtagsLifecycleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EtagsLifecycleTests(WebApplicationFactory<Program> factory)
    {
        // Configure factory to run in the 'Testing' environment (uses InMemory DB)
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");
            builder.ConfigureServices(services =>
            {
                // Replace file validation with a no-op so we don't need real files on disk
                services.AddSingleton<IFileValidationService, NoOpFileValidationService>();
            });
        });
    }

    [Fact]
    public async Task Post_Get_Patch_Delete_EtagLifecycle()
    {
        var client = _factory.CreateClient();

        // Create simulation payload
        var create = new CreateSimulationRequest
        {
            Name = "integration-test-sim",
            // Use a far-future start time to satisfy validators
            StartTime = "2100-01-01T00:00:00Z",
            DataSource = "C:\\nonexistent\\dummy.csv"
        };

        // POST -> Created
        var postResp = await client.PostAsJsonAsync("/api/simulations", create);
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);
        postResp.Headers.Location.Should().NotBeNull();
        var location = postResp.Headers.Location!.ToString();

        // GET -> 200 and returns ETag header
        var getResp = await client.GetAsync(location);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Headers.TryGetValues("ETag", out var etagValues).Should().BeTrue();
        var etagRaw = etagValues!.First();
        NormalizeEntityTag(etagRaw).Should().NotBeNullOrWhiteSpace();

        // PATCH -> send If-Match header with the quoted ETag exactly as returned by GET
        var update = new UpdateSimulationRequest { Name = "updated-name-v1" };
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, location)
        {
            Content = JsonContent.Create(update)
        };
        patchReq.Headers.TryAddWithoutValidation("If-Match", etagRaw);
        var patchResp = await client.SendAsync(patchReq);
        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        patchResp.Headers.TryGetValues("ETag", out var newEtagValues).Should().BeTrue();
        var newEtagRaw = newEtagValues!.First();
        NormalizeEntityTag(newEtagRaw).Should().NotBeNullOrWhiteSpace();
        NormalizeEntityTag(newEtagRaw).Should().NotBe(NormalizeEntityTag(etagRaw));

        // DELETE -> provide latest ETag in If-Match -> expect NoContent
        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, location);
        deleteReq.Headers.TryAddWithoutValidation("If-Match", newEtagRaw);
        var deleteResp = await client.SendAsync(deleteReq);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET after delete -> NotFound
        var getAfter = await client.GetAsync(location);
        getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string NormalizeEntityTag(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var v = raw.Trim();
        if (v.StartsWith("W/", StringComparison.OrdinalIgnoreCase)) v = v.Substring(2);
        if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\"")) v = v.Substring(1, v.Length - 2);
        return v;
    }

    private class NoOpFileValidationService : IFileValidationService
    {
        public Task ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
