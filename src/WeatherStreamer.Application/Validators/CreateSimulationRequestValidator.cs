using FluentValidation;
using WeatherStreamer.Application.DTOs;

namespace WeatherStreamer.Application.Validators;

/// <summary>
/// Validator for CreateSimulationRequest with comprehensive validation rules.
/// </summary>
public class CreateSimulationRequestValidator : AbstractValidator<CreateSimulationRequest>
{
    public CreateSimulationRequestValidator()
    {
        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required and cannot be empty")
            .MaximumLength(70).WithMessage("Name cannot exceed 70 characters");

        // StartTime validation
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required")
            .Must(BeValidIso8601DateTime).WithMessage("StartTime must be in ISO 8601 format (e.g., 2025-11-10T14:30:00Z)");

        // DataSource validation
        RuleFor(x => x.DataSource)
            .NotEmpty().WithMessage("DataSource is required and cannot be empty")
            .MaximumLength(260).WithMessage("File path cannot exceed 260 characters (Windows MAX_PATH limit)")
            .Must(NotStartWithDigit).WithMessage("File name cannot start with a numeric digit")
            .Must(ContainOnlyValidCharacters).WithMessage("File path can only contain alphanumeric characters, spaces, hyphens, underscores, periods, and backslashes");
    }

    private bool BeValidIso8601DateTime(string dateTimeString)
    {
        return DateTime.TryParse(dateTimeString, out _);
    }

    private bool NotStartWithDigit(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
            return true;

        return !char.IsDigit(fileName[0]);
    }

    private bool ContainOnlyValidCharacters(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        // Allow: alphanumeric, spaces, hyphens, underscores, periods, backslashes
        foreach (var c in filePath)
        {
            if (!char.IsLetterOrDigit(c) && 
                c != ' ' && 
                c != '-' && 
                c != '_' && 
                c != '.' && 
                c != '\\' &&
                c != ':') // Allow colon for drive letters (C:\)
            {
                return false;
            }
        }

        return true;
    }
}
