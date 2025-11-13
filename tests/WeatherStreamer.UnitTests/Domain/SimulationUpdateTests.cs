using System;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace WeatherStreamer.UnitTests.Domain
{
    public class SimulationUpdateTests
    {
        [Fact]
        public void ApplyUpdate_Allows_NotStarted_To_InProgress()
        {
            var s = new Simulation { Id = 1, Name = "S", StartTime = DateTime.UtcNow.AddHours(1), FileName = "file.csv", Status = SimulationStatus.NotStarted };
            s.ApplyUpdate(null, null, null, SimulationStatus.InProgress);
            s.Status.Should().Be(SimulationStatus.InProgress);
        }

        [Fact]
        public void ApplyUpdate_Disallows_NotStarted_To_Completed()
        {
            var s = new Simulation { Id = 1, Name = "S", StartTime = DateTime.UtcNow.AddHours(1), FileName = "file.csv", Status = SimulationStatus.NotStarted };
            Action act = () => s.ApplyUpdate(null, null, null, SimulationStatus.Completed);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Not Started cannot jump directly to Completed*");
        }

        [Fact]
        public void ApplyUpdate_Disallows_Backwards_Transition()
        {
            var s = new Simulation { Id = 1, Name = "S", StartTime = DateTime.UtcNow.AddHours(1), FileName = "file.csv", Status = SimulationStatus.Completed };
            Action act = () => s.ApplyUpdate(null, null, null, SimulationStatus.InProgress);
            act.Should().Throw<InvalidOperationException>().WithMessage("*cannot move to a previous state*");
        }

        [Fact]
        public void ApplyUpdate_Disallows_FileName_Change_When_InProgress()
        {
            var s = new Simulation { Id = 1, Name = "S", StartTime = DateTime.UtcNow.AddHours(-1), FileName = "file.csv", Status = SimulationStatus.InProgress };
            Action act = () => s.ApplyUpdate(null, null, "other.csv", null);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot change DataSource after the simulation has started.*");
        }
    }
}
