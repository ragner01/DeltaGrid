using System.Collections.Concurrent;
using IOC.Application.Allocation;
using IOC.Core.Domain.Allocation;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryBatteryRepository : IBatteryRepository
{
    private static readonly ConcurrentDictionary<string, Battery> Batteries = new();

    public Task<Battery?> GetAsync(string batteryId, CancellationToken ct)
    {
        Batteries.TryGetValue(batteryId, out var b);
        return Task.FromResult(b);
    }

    public static void Seed(string batteryId, string site, string asset, IEnumerable<string> wellIds)
    {
        Batteries[batteryId] = new Battery(batteryId, site, asset, wellIds);
    }
}

public sealed class InMemoryMeterReadingRepository : IMeterReadingRepository
{
    private static readonly ConcurrentDictionary<(string, DateOnly), BatteryMeasurement> Data = new();

    public Task<BatteryMeasurement?> GetBatteryMeasurementAsync(string batteryId, DateOnly day, CancellationToken ct)
    {
        Data.TryGetValue((batteryId, day), out var m);
        return Task.FromResult<BatteryMeasurement?>(m);
    }

    public static void Seed(BatteryMeasurement m) => Data[(m.BatteryId, m.Day)] = m;
}

public sealed class InMemoryWellTestRepository : IWellTestRepository
{
    private static readonly ConcurrentDictionary<(string, DateOnly), WellTest> Tests = new();

    public Task<IReadOnlyList<WellTest>> GetTestsAsync(IEnumerable<string> wellIds, DateOnly day, CancellationToken ct)
    {
        var list = new List<WellTest>();
        foreach (var id in wellIds)
        {
            if (Tests.TryGetValue((id, day), out var t)) list.Add(t);
        }
        return Task.FromResult((IReadOnlyList<WellTest>)list);
    }

    public static void Seed(WellTest t) => Tests[(t.WellId, t.Day)] = t;
}

public sealed class InMemoryAllocationRunRepository : IAllocationRunRepository, IOC.Application.Allocation.Reconcile.IAllocationReadRepository
{
    private static readonly ConcurrentDictionary<(string, DateOnly), int> Versions = new();
    private static readonly ConcurrentDictionary<Guid, AllocationRun> Runs = new();
    private static readonly ConcurrentDictionary<(string, DateOnly, int), List<AllocationResult>> ResultsByKey = new();

    public Task<int> GetNextVersionAsync(string batteryId, DateOnly day, CancellationToken ct)
    {
        int next = Versions.AddOrUpdate((batteryId, day), 1, (_, v) => v + 1);
        return Task.FromResult(next);
    }

    public Task SaveAsync(AllocationRun run, CancellationToken ct)
    {
        Runs[run.RunId] = run;
        ResultsByKey[(run.BatteryId, run.Day, run.Version)] = run.Results.ToList();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AllocationResult>> GetResultsAsync(string batteryId, DateOnly day, int version, CancellationToken ct)
    {
        ResultsByKey.TryGetValue((batteryId, day, version), out var list);
        return Task.FromResult((IReadOnlyList<AllocationResult>)(list ?? new List<AllocationResult>()));
    }
}
