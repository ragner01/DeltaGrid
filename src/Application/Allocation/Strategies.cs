using IOC.Core.Domain.Allocation;

namespace IOC.Application.Allocation;

public interface IAllocationStrategy
{
    string Method { get; }
    IReadOnlyList<AllocationResult> Allocate(DateOnly day, Battery battery, BatteryMeasurement meas, IReadOnlyList<WellTest> tests, int version);
}

public sealed class ProportionalByTestStrategy : IAllocationStrategy
{
    public string Method => "ProportionalByTest";

    public IReadOnlyList<AllocationResult> Allocate(DateOnly day, Battery battery, BatteryMeasurement meas, IReadOnlyList<WellTest> tests, int version)
    {
        var testsByWell = tests.ToDictionary(t => t.WellId, t => t);
        double totalOil = tests.Where(t => battery.WellIds.Contains(t.WellId)).Sum(t => Math.Max(t.OilRate_m3_d, 0));
        double totalGas = tests.Where(t => battery.WellIds.Contains(t.WellId)).Sum(t => Math.Max(t.GasRate_m3_d, 0));
        double totalWater = tests.Where(t => battery.WellIds.Contains(t.WellId)).Sum(t => Math.Max(t.WaterRate_m3_d, 0));

        var results = new List<AllocationResult>();
        foreach (var wellId in battery.WellIds)
        {
            testsByWell.TryGetValue(wellId, out var wt);
            var oilShare = totalOil <= 0 ? 0 : (Math.Max(wt?.OilRate_m3_d ?? 0, 0) / totalOil);
            var gasShare = totalGas <= 0 ? 0 : (Math.Max(wt?.GasRate_m3_d ?? 0, 0) / totalGas);
            var waterShare = totalWater <= 0 ? 0 : (Math.Max(wt?.WaterRate_m3_d ?? 0, 0) / totalWater);
            results.Add(new AllocationResult(wellId, day, Round(meas.OilMeasured_m3 * oilShare), Round(meas.GasMeasured_m3 * gasShare), Round(meas.WaterMeasured_m3 * waterShare), Method, version));
        }
        // deterministic rounding adjustment: fix by distributing remainder to wells in order
        results = AdjustRounding(results, meas);
        return results;
    }

    private static double Round(double v) => Math.Round(v, 3, MidpointRounding.ToZero);

    private static List<AllocationResult> AdjustRounding(List<AllocationResult> results, BatteryMeasurement meas)
    {
        double oilDelta = Math.Round(meas.OilMeasured_m3 - results.Sum(r => r.Oil_m3), 3, MidpointRounding.ToZero);
        double gasDelta = Math.Round(meas.GasMeasured_m3 - results.Sum(r => r.Gas_m3), 3, MidpointRounding.ToZero);
        double waterDelta = Math.Round(meas.WaterMeasured_m3 - results.Sum(r => r.Water_m3), 3, MidpointRounding.ToZero);
        List<AllocationResult> adjusted = new();
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var oil = r.Oil_m3 + (oilDelta > 0 && i < oilDelta * 1000 ? 0.001 : 0);
            var gas = r.Gas_m3 + (gasDelta > 0 && i < gasDelta * 1000 ? 0.001 : 0);
            var water = r.Water_m3 + (waterDelta > 0 && i < waterDelta * 1000 ? 0.001 : 0);
            adjusted.Add(r with { Oil_m3 = oil, Gas_m3 = gas, Water_m3 = water });
        }
        return adjusted;
    }
}
