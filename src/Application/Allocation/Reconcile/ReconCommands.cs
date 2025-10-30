using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Allocation;

namespace IOC.Application.Allocation.Reconcile;

public sealed record ReconcileAllocationCommand(DateOnly Day, string BatteryId, double VariancePctThreshold) : IRequest<Result<ReconciliationSummary>>;

public sealed record ReconciliationSummary(string BatteryId, DateOnly Day, double OilVariancePct, double GasVariancePct, double WaterVariancePct, bool WithinThreshold);

public interface IAllocationReadRepository
{
    Task<IReadOnlyList<AllocationResult>> GetResultsAsync(string batteryId, DateOnly day, int version, CancellationToken ct);
}

public sealed class ReconcileAllocationHandler : IRequestHandler<ReconcileAllocationCommand, Result<ReconciliationSummary>>
{
    private readonly IMeterReadingRepository _meters;
    private readonly IAllocationRunRepository _runs;
    private readonly IAllocationReadRepository _reads;

    public ReconcileAllocationHandler(IMeterReadingRepository meters, IAllocationRunRepository runs, IAllocationReadRepository reads)
    {
        _meters = meters; _runs = runs; _reads = reads;
    }

    public async Task<Result<ReconciliationSummary>> Handle(ReconcileAllocationCommand request, CancellationToken cancellationToken)
    {
        var meas = await _meters.GetBatteryMeasurementAsync(request.BatteryId, request.Day, cancellationToken);
        if (meas is null) return Result<ReconciliationSummary>.Failure("Measurement not found");
        // assume latest version
        var version = await _runs.GetNextVersionAsync(request.BatteryId, request.Day, cancellationToken) - 1;
        if (version <= 0) return Result<ReconciliationSummary>.Failure("No allocation run found");
        var results = await _reads.GetResultsAsync(request.BatteryId, request.Day, version, cancellationToken);
        if (results.Count == 0) return Result<ReconciliationSummary>.Failure("No allocation results found");

        double oilAlloc = results.Sum(r => r.Oil_m3);
        double gasAlloc = results.Sum(r => r.Gas_m3);
        double waterAlloc = results.Sum(r => r.Water_m3);

        double oilVar = PercentVariance(oilAlloc, meas.OilMeasured_m3);
        double gasVar = PercentVariance(gasAlloc, meas.GasMeasured_m3);
        double waterVar = PercentVariance(waterAlloc, meas.WaterMeasured_m3);

        bool ok = Math.Abs(oilVar) <= request.VariancePctThreshold && Math.Abs(gasVar) <= request.VariancePctThreshold && Math.Abs(waterVar) <= request.VariancePctThreshold;
        return Result<ReconciliationSummary>.Success(new ReconciliationSummary(request.BatteryId, request.Day, oilVar, gasVar, waterVar, ok));
    }

    private static double PercentVariance(double alloc, double measured)
    {
        if (measured == 0) return alloc == 0 ? 0 : 100;
        return Math.Round(((alloc - measured) / measured) * 100.0, 3);
    }
}
