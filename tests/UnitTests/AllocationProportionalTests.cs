using FluentAssertions;
using IOC.Application.Allocation;
using IOC.Core.Domain.Allocation;

namespace IOC.UnitTests;

public class AllocationProportionalTests
{
    [Fact]
    public void Mass_Balance_Holds_With_Rounding_Adjustment()
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var battery = new Battery("bat-1", "site-1", "asset-1", new[] { "well-a", "well-b", "well-c" });
        var meas = new BatteryMeasurement(battery.BatteryId, day, 30.0, 0.0, 0.0);
        var tests = new List<WellTest>
        {
            new("well-a", day, 10, 0, 0),
            new("well-b", day, 15, 0, 0),
            new("well-c", day, 5, 0, 0)
        };

        var strat = new ProportionalByTestStrategy();
        var results = strat.Allocate(day, battery, meas, tests, version: 1);

        var sumOil = Math.Round(results.Sum(r => r.Oil_m3), 3, MidpointRounding.ToZero);
        sumOil.Should().Be(meas.OilMeasured_m3);
    }
}
