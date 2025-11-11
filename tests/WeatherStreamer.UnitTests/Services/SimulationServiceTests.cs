using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Repositories;
using WeatherStreamer.Application.Services;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using WeatherStreamer.Infrastructure.Services;

namespace WeatherStreamer.UnitTests.Services;

/// <summary>
/// Unit tests for SimulationService.
/// </summary>
public class SimulationServiceTests
{
    private readonly Mock<ISimulationRepository> _mockRepository;
    private readonly Mock<IFileValidationService> _mockFileValidation;
    private readonly Mock<ILogger<SimulationService>> _mockLogger;
    private readonly SimulationService _service;

    public SimulationServiceTests()
    {
        _mockRepository = new Mock<ISimulationRepository>();
        _mockFileValidation = new Mock<IFileValidationService>();
        _mockLogger = new Mock<ILogger<SimulationService>>();
        _service = new SimulationService(
            _mockRepository.Object,
            _mockFileValidation.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateSimulationAsync_WithValidRequest_ReturnsSimulationId()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\sample.csv"
        };

        var createdSimulation = new Simulation
        {
            Id = 1,
            Name = request.Name,
            StartTime = DateTime.Parse(request.StartTime).ToUniversalTime(),
            FileName = request.DataSource,
            Status = SimulationStatus.NotStarted
        };

        _mockFileValidation
            .Setup(x => x.ValidateFileAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.IsFileInUseAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Simulation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSimulation);

        // Act
        var simulationId = await _service.CreateSimulationAsync(request, CancellationToken.None);

        // Assert
        simulationId.Should().Be(1);

        // Verify repository was called with correct data
        _mockRepository.Verify(
            x => x.CreateAsync(
                It.Is<Simulation>(s =>
                    s.Name == request.Name &&
                    s.FileName == request.DataSource &&
                    s.Status == SimulationStatus.NotStarted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSimulationAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\nonexistent.csv"
        };

        _mockFileValidation
            .Setup(x => x.ValidateFileAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        Func<Task> act = async () => await _service.CreateSimulationAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("File not found");
    }

    [Fact]
    public async Task CreateSimulationAsync_FileInUse_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\sample.csv"
        };

        _mockFileValidation
            .Setup(x => x.ValidateFileAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.IsFileInUseAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.CreateSimulationAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*in use*");
    }

    [Fact]
    public async Task CreateSimulationAsync_ConvertsStartTimeToUtc()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00+05:00", // UTC+5
            DataSource = @"C:\test-data\sample.csv"
        };

        Simulation capturedSimulation = null!;

        _mockFileValidation
            .Setup(x => x.ValidateFileAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.IsFileInUseAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Simulation>(), It.IsAny<CancellationToken>()))
            .Callback<Simulation, CancellationToken>((sim, ct) => capturedSimulation = sim)
            .ReturnsAsync((Simulation s, CancellationToken ct) => { s.Id = 1; return s; });

        // Act
        await _service.CreateSimulationAsync(request, CancellationToken.None);

        // Assert
        capturedSimulation.Should().NotBeNull();
        capturedSimulation.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        capturedSimulation.StartTime.Should().Be(new DateTime(2025, 1, 15, 5, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CreateSimulationAsync_WithDatabaseError_ThrowsAndLogsException()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\weather.csv"
        };

        _mockFileValidation
            .Setup(x => x.ValidateFileAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(x => x.IsFileInUseAsync(request.DataSource, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Simulate database error from repository
        var dbException = new InvalidOperationException(
            "Failed to create simulation due to a database error. This may be due to a constraint violation or connectivity issue.");
        
        _mockRepository
            .Setup(x => x.CreateAsync(It.IsAny<Simulation>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbException);

        // Act
        Func<Task> act = async () => await _service.CreateSimulationAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*database error*");
    }
}
