using System;
using FluentAssertions;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.UnitTests.Domain;

public class SimulationTransitionTests
{
    [Fact]
    public void NotStarted_To_InProgress_Is_Allowed()
    {
        var sim = new Simulation { Status = SimulationStatus.NotStarted };
        Action act = () => sim.ApplyUpdate(null, null, null, SimulationStatus.InProgress);
        act.Should().NotThrow();
        sim.Status.Should().Be(SimulationStatus.InProgress);
    }

    [Fact]
    public void InProgress_To_Completed_Is_Allowed()
    {
        var sim = new Simulation { Status = SimulationStatus.InProgress };
        Action act = () => sim.ApplyUpdate(null, null, null, SimulationStatus.Completed);
        act.Should().NotThrow();
        sim.Status.Should().Be(SimulationStatus.Completed);
    }

    [Fact]
    public void Cannot_Skip_NotStarted_To_Completed()
    {
        var sim = new Simulation { Status = SimulationStatus.NotStarted };
        Action act = () => sim.ApplyUpdate(null, null, null, SimulationStatus.Completed);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Illegal status transition: Not Started cannot jump directly to Completed.");
    }

    [Fact]
    public void Cannot_Transition_Backwards()
    {
        var sim = new Simulation { Status = SimulationStatus.Completed };
        Action act = () => sim.ApplyUpdate(null, null, null, SimulationStatus.InProgress);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Illegal status transition: cannot move to a previous state.");
    }
}
