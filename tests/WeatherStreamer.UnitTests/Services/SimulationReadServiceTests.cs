using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Services.Simulations;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.UnitTests.Services;

public class SimulationReadServiceTests
{
    private readonly Mock<ISimulationReadRepository> _repoMock = new();
    private readonly SimulationReadService _service;
    private readonly List<Simulation> _seed;

    public SimulationReadServiceTests()
    {
        _seed = new List<Simulation>
        {
            new() { Id = 2, Name = "B", StartTime = new DateTime(2025,12,01,10,00,00, DateTimeKind.Utc), FileName = "b.csv", Status = SimulationStatus.NotStarted },
            new() { Id = 1, Name = "A", StartTime = new DateTime(2025,12,01,09,00,00, DateTimeKind.Utc), FileName = "a.csv", Status = SimulationStatus.InProgress },
            new() { Id = 3, Name = "C", StartTime = new DateTime(2025,12,01,10,00,01, DateTimeKind.Utc), FileName = "c.csv", Status = SimulationStatus.Completed }
        };
        var loggerMock = new Mock<ILogger<SimulationReadService>>();
        _service = new SimulationReadService(_repoMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartTimeThenId()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_seed);

    var result = await _service.GetAllAsync();
    result.Select(r => r.Id).Should().Equal(new[] { 1, 2, 3 });
        result[0].StartTimeUtc.Should().Be(new DateTime(2025,12,01,09,00,00, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetFromStartTimeAsync_FiltersInclusiveBoundary()
    {
        var boundary = new DateTime(2025,12,01,10,00,00, DateTimeKind.Utc);
        _repoMock.Setup(r => r.GetFromStartTimeAsync(boundary, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_seed.Where(s => s.StartTime >= boundary).ToList());

        var result = await _service.GetFromStartTimeAsync(boundary);
        result.Select(r => r.Id).Should().Equal(2,3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Simulation?)null);

        var result = await _service.GetByIdAsync(99);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsWhenIdInvalid()
    {
        Func<Task> act = () => _service.GetByIdAsync(0);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
