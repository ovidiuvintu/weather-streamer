namespace WeatherStreamer.Application.Services;

/// <summary>
/// Service interface for file system validation operations.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates that a file exists and is accessible.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when validation is done.</returns>
    /// <exception cref="FileNotFoundException">Thrown when file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory does not exist.</exception>
    /// <exception cref="IOException">Thrown when file is locked.</exception>
    Task ValidateFileAsync(string filePath, CancellationToken cancellationToken = default);
}
