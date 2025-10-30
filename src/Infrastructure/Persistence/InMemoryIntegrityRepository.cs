using System.Collections.Concurrent;
using IOC.Application.Integrity;
using IOC.Core.Domain.Integrity;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryIntegrityRepository : IIntegrityRepository
{
    private static readonly ConcurrentDictionary<(string eq, string loc), List<ThicknessReading>> Readings = new();
    private static readonly ConcurrentDictionary<string, InspectionPlan> Plans = new();
    private static readonly ConcurrentDictionary<Guid, Anomaly> Anomalies = new();

    public Task SaveReadingAsync(ThicknessReading r, CancellationToken ct)
    {
        var list = Readings.GetOrAdd((r.EquipmentId, r.Location), _ => new List<ThicknessReading>());
        list.Add(r);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ThicknessReading>> GetReadingsAsync(string equipmentId, string location, CancellationToken ct)
    {
        Readings.TryGetValue((equipmentId, location), out var list);
        return Task.FromResult((IReadOnlyList<ThicknessReading>)(list ?? new List<ThicknessReading>()));
    }

    public Task SavePlanAsync(InspectionPlan plan, CancellationToken ct)
    {
        Plans[plan.PlanId] = plan;
        return Task.CompletedTask;
    }

    public Task<InspectionPlan?> GetPlanAsync(string planId, CancellationToken ct)
    {
        Plans.TryGetValue(planId, out var p);
        return Task.FromResult(p);
    }

    public Task SaveAnomalyAsync(Anomaly anomaly, CancellationToken ct)
    {
        Anomalies[anomaly.Id] = anomaly;
        return Task.CompletedTask;
    }

    public Task<Anomaly?> GetAnomalyAsync(Guid id, CancellationToken ct)
    {
        Anomalies.TryGetValue(id, out var a);
        return Task.FromResult(a);
    }
}
