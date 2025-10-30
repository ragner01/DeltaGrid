using IOC.Core.Domain.Allocation;

namespace IOC.Application.Allocation;

public interface IBatteryRepository
{
    Task<Battery?> GetAsync(string batteryId, CancellationToken ct);
}

public interface IMeterReadingRepository
{
    Task<BatteryMeasurement?> GetBatteryMeasurementAsync(string batteryId, DateOnly day, CancellationToken ct);
}

public interface IWellTestRepository
{
    Task<IReadOnlyList<WellTest>> GetTestsAsync(IEnumerable<string> wellIds, DateOnly day, CancellationToken ct);
}

public interface IAllocationRunRepository
{
    Task<int> GetNextVersionAsync(string batteryId, DateOnly day, CancellationToken ct);
    Task SaveAsync(AllocationRun run, CancellationToken ct);
}
