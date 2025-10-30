using FluentAssertions;
using IOC.Core.Domain.Well;

namespace IOC.UnitTests;

public class WellStateEngineTests
{
    [Fact]
    public void Illegal_Transition_Blocked()
    {
        var well = Well.Create("W1", LiftMethod.Natural, new Limits(0, 100, TimeSpan.FromMinutes(10)));
        var res = well.ChangeState(WellState.Stable, DateTimeOffset.UtcNow);
        res.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Requires_Stabilization_Time()
    {
        var well = Well.Create("W1", LiftMethod.Natural, new Limits(0, 100, TimeSpan.FromMinutes(10)));
        well.ChangeState(WellState.RampUp, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        var res = well.ChangeState(WellState.Stable, DateTimeOffset.UtcNow.AddMinutes(5));
        res.IsSuccess.Should().BeFalse();
    }
}
