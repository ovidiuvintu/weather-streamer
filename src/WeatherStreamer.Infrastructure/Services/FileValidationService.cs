using WeatherStreamer.Application.Services;

namespace WeatherStreamer.Infrastructure.Services;

/// <summary>
/// Implementation of file validation service for file system checks.
/// </summary>
public class FileValidationService : IFileValidationService
{
    /// <inheritdoc />
    public Task ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        // Check if directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The directory '{directory}' does not exist.");
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' does not exist.", filePath);
        }

        // Check if file is locked by trying to open it
        try
        {
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (IOException ex) when (ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"The file '{filePath}' is currently locked by another process. Please retry later.", ex);
        }

        return Task.CompletedTask;
    }
}
