using FluentValidation;
using WeatherStreamer.Application.Services.Simulations.Update;
using System.IO;

namespace WeatherStreamer.Application.Validators;

public class UpdateSimulationCommandValidator : AbstractValidator<UpdateSimulationCommand>
{
    public UpdateSimulationCommandValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required and cannot be empty")
                .MaximumLength(70).WithMessage("Name cannot exceed 70 characters");
        });

        When(x => x.StartTime != null, () =>
        {
            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("StartTime cannot be empty")
                .Must(BeValidIso8601DateTime).WithMessage("StartTime must be in ISO 8601 format (e.g., 2025-11-10T14:30:00Z)")
                .Must(BeInFuture).WithMessage("StartTime must be in the future");
        });

        When(x => x.DataSource != null, () =>
        {
            RuleFor(x => x.DataSource)
                .NotEmpty().WithMessage("DataSource cannot be empty")
                .MaximumLength(260).WithMessage("File path cannot exceed 260 characters (Windows MAX_PATH limit)")
                .Must(NotStartWithDigit).WithMessage("File name cannot start with a numeric digit")
                .Must(ContainOnlyValidCharacters).WithMessage("File path can only contain alphanumeric characters, spaces, hyphens, underscores, periods, colons and backslashes");
        });

        RuleFor(x => x.IfMatch)
            .NotEmpty().WithMessage("If-Match header (version) is required for updates");
    }

    private bool BeValidIso8601DateTime(string? dateTimeString)
    {
        return DateTime.TryParse(dateTimeString, out _);
    }

    private bool BeInFuture(string? dateTimeString)
    {
        if (!DateTime.TryParse(dateTimeString, out var startTime))
            return true; // Let BeValidIso8601DateTime handle invalid format

        if (startTime.Kind != DateTimeKind.Utc)
        {
            startTime = startTime.ToUniversalTime();
        }

        var currentTime = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            DateTime.UtcNow.Day,
            DateTime.UtcNow.Hour,
            DateTime.UtcNow.Minute,
            DateTime.UtcNow.Second,
            DateTimeKind.Utc);

        var startTimeToSecond = new DateTime(
            startTime.Year,
            startTime.Month,
            startTime.Day,
            startTime.Hour,
            startTime.Minute,
            startTime.Second,
            DateTimeKind.Utc);

        return startTimeToSecond > currentTime;
    }

    private static bool NotStartWithDigit(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
            return true;

        return !char.IsDigit(fileName[0]);
    }

    private static bool ContainOnlyValidCharacters(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        foreach (var c in filePath)
        {
            if (!char.IsLetterOrDigit(c) &&
                c != ' ' &&
                c != '-' &&
                c != '_' &&
                c != '.' &&
                c != '\\' &&
                c != ':')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Public helper to validate a candidate data source path independently.
    /// Used by handlers that need to make decisions based on the current domain status.
    /// </summary>
    public static bool IsValidDataSource(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (filePath.Length > 260) return false;
        if (!NotStartWithDigit(filePath)) return false;
        if (!ContainOnlyValidCharacters(filePath)) return false;
        return true;
    }
}
