using FluentAssertions;
using WeatherStreamer.Application.DTOs;
using WeatherStreamer.Application.Validators;

namespace WeatherStreamer.UnitTests.Validators;

/// <summary>
/// Unit tests for CreateSimulationRequestValidator.
/// </summary>
public class CreateSimulationRequestValidatorTests
{
    private readonly CreateSimulationRequestValidator _validator;

    public CreateSimulationRequestValidatorTests()
    {
        _validator = new CreateSimulationRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_PassesValidation()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Valid Simulation Name",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingName_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
        result.Errors.First(e => e.PropertyName == "Name")
            .ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void Validate_WithNameTooLong_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = new string('A', 71), // 71 characters, exceeds 70 limit
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
        result.Errors.First(e => e.PropertyName == "Name")
            .ErrorMessage.Should().Contain("70");
    }

    [Fact]
    public void Validate_WithInvalidStartTimeFormat_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "15/01/2025 10:30", // Invalid format
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "StartTime");
        result.Errors.First(e => e.PropertyName == "StartTime")
            .ErrorMessage.Should().Contain("ISO 8601");
    }

    [Fact]
    public void Validate_WithMissingStartTime_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "",
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime");
    }

    [Fact]
    public void Validate_WithMissingDataSource_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "DataSource");
        result.Errors.First(e => e.PropertyName == "DataSource")
            .ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void Validate_WithDataSourceTooLong_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = new string('A', 261) // 261 characters, exceeds 260 limit
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "DataSource");
        result.Errors.First(e => e.PropertyName == "DataSource")
            .ErrorMessage.Should().Contain("260");
    }

    [Fact]
    public void Validate_WithNumericFilenamePrefix_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\123file.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "DataSource");
        result.Errors.First(e => e.PropertyName == "DataSource")
            .ErrorMessage.Should().Contain("numeric");
    }

    [Fact]
    public void Validate_WithInvalidCharactersInDataSource_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = "2025-01-15T10:30:00Z",
            DataSource = @"C:\test-data\file@#$.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "DataSource");
        result.Errors.First(e => e.PropertyName == "DataSource")
            .ErrorMessage.Should().Contain("alphanumeric");
    }

    [Theory]
    [InlineData("2025-01-15T10:30:00Z")]
    [InlineData("2025-01-15T10:30:00+05:00")]
    [InlineData("2025-01-15T10:30:00-08:00")]
    [InlineData("2025-01-15T10:30:00.123Z")]
    public void Validate_WithVariousValidIso8601Formats_PassesValidation(string startTime)
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            StartTime = startTime,
            DataSource = @"C:\test-data\sample.csv"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
