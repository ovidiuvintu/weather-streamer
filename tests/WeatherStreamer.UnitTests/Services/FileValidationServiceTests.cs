using FluentAssertions;
using WeatherStreamer.Infrastructure.Services;

namespace WeatherStreamer.UnitTests.Services;

/// <summary>
/// Unit tests for FileValidationService.
/// </summary>
public class FileValidationServiceTests
{
    private readonly FileValidationService _service;

    public FileValidationServiceTests()
    {
        _service = new FileValidationService();
    }

    [Fact]
    public async Task ValidateFileAsync_WithNonExistentPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\Path\file.csv";

        // Act
        Func<Task> act = async () => await _service.ValidateFileAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task ValidateFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        // Use a valid directory but non-existent file
        var tempDir = Path.GetTempPath();
        var nonExistentFile = Path.Combine(tempDir, $"nonexistent_{Guid.NewGuid()}.csv");

        // Act
        Func<Task> act = async () => await _service.ValidateFileAsync(nonExistentFile);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task ValidateFileAsync_WithLockedFile_ThrowsIOException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            // Lock the file by opening it exclusively
            using var lockStream = File.Open(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            
            // Act
            Func<Task> act = async () => await _service.ValidateFileAsync(tempFile);

            // Assert
            await act.Should().ThrowAsync<IOException>()
                .WithMessage("*locked*");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithValidAccessibleFile_Succeeds()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            File.WriteAllText(tempFile, "test data");

            // Act
            Func<Task> act = async () => await _service.ValidateFileAsync(tempFile);

            // Assert
            await act.Should().NotThrowAsync();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ValidateFileAsync_WithNullOrEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert - null path
        Func<Task> actNull = async () => await _service.ValidateFileAsync(null!);
        await actNull.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");

        // Act & Assert - empty path
        Func<Task> actEmpty = async () => await _service.ValidateFileAsync(string.Empty);
        await actEmpty.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }
}
