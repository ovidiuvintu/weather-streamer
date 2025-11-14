namespace WeatherStreamer.Application.Services.Simulations.Delete;

public class DeleteSimulationCommand
{
    public int Id { get; init; }

    /// <summary>
    /// Base64 encoded rowversion token supplied via If-Match header.
    /// </summary>
    public string IfMatch { get; init; } = string.Empty;

    public string? Actor { get; init; }

    public string? CorrelationId { get; init; }
}
