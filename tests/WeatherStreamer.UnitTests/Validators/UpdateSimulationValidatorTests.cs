using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using WeatherStreamer.Application.Services.Simulations.Update;
using WeatherStreamer.Application.Validators;

namespace WeatherStreamer.UnitTests.Validators;

public class UpdateSimulationValidatorTests
{
    [Fact]
    public async Task Allows_NameOnly_Payload()
    {
        var validator = new UpdateSimulationCommandValidator();
        var cmd = new UpdateSimulationCommand { Name = "Valid", IfMatch = "dGVzdA==" };
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue("Name-only updates should be allowed");
    }

    [Fact]
    public async Task Rejects_Name_TooLong()
    {
        var validator = new UpdateSimulationCommandValidator();
        var longName = new string('A', 71);
        var cmd = new UpdateSimulationCommand { Name = longName, IfMatch = "dGVzdA==" };
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
