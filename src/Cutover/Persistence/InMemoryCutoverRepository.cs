using IOC.Cutover.Models;
using IOC.Cutover.Services;

namespace IOC.Cutover.Persistence;

/// <summary>
/// In-memory implementation of cutover repositories
/// </summary>
public sealed class InMemoryCutoverRepository :
    ISeedDataRepository,
    ICutoverRepository,
    IFeatureFlagRepository,
    IHypercareRepository
{
    private readonly Dictionary<string, SeedData> _seedData = new();
    private readonly Dictionary<string, CutoverExecution> _cutovers = new();
    private readonly Dictionary<string, RollbackPlan> _rollbackPlans = new();
    private readonly Dictionary<string, ReadinessCriteria> _readinessCriteria = new();
    private readonly Dictionary<string, FeatureFlag> _featureFlags = new();
    private readonly Dictionary<string, HypercareIncident> _incidents = new();

    // ISeedDataRepository
    public Task SaveSeedDataAsync(SeedData data, CancellationToken ct = default)
    {
        _seedData[data.Id] = data;
        return Task.CompletedTask;
    }

    public Task<int> GetSeedDataCountAsync(SeedDataType type, CancellationToken ct = default)
    {
        var count = _seedData.Values.Count(d => d.Type == type);
        return Task.FromResult(count);
    }

    public Task ClearSeedDataAsync(SeedDataType type, CancellationToken ct = default)
    {
        var keysToRemove = _seedData.Where(kvp => kvp.Value.Type == type).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _seedData.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<List<SeedData>> GetSeedDataAsync(SeedDataType type, CancellationToken ct = default)
    {
        return Task.FromResult(_seedData.Values.Where(d => d.Type == type).ToList());
    }

    // ICutoverRepository
    public Task SaveCutoverAsync(CutoverExecution cutover, CancellationToken ct = default)
    {
        _cutovers[cutover.Id] = cutover;
        return Task.CompletedTask;
    }

    public Task<CutoverExecution?> GetCutoverAsync(string cutoverId, CancellationToken ct = default)
    {
        return Task.FromResult(_cutovers.TryGetValue(cutoverId, out var cutover) ? cutover : null);
    }

    public Task<List<ReadinessCriteria>> GetReadinessCriteriaAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_readinessCriteria.Values.ToList());
    }

    public Task SaveRollbackPlanAsync(RollbackPlan plan, CancellationToken ct = default)
    {
        _rollbackPlans[plan.Id] = plan;
        return Task.CompletedTask;
    }

    public Task<RollbackPlan?> GetRollbackPlanAsync(string planId, CancellationToken ct = default)
    {
        return Task.FromResult(_rollbackPlans.TryGetValue(planId, out var plan) ? plan : null);
    }

    // IFeatureFlagRepository
    public Task SaveFlagAsync(FeatureFlag flag, CancellationToken ct = default)
    {
        _featureFlags[flag.Id] = flag;
        return Task.CompletedTask;
    }

    public Task<FeatureFlag?> GetFlagAsync(string flagId, CancellationToken ct = default)
    {
        return Task.FromResult(_featureFlags.TryGetValue(flagId, out var flag) ? flag : null);
    }

    public Task<List<FeatureFlag>> GetEnabledFlagsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_featureFlags.Values.Where(f => f.IsEnabled).ToList());
    }

    public Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_featureFlags.Values.ToList());
    }

    // IHypercareRepository
    public Task SaveIncidentAsync(HypercareIncident incident, CancellationToken ct = default)
    {
        _incidents[incident.Id] = incident;
        return Task.CompletedTask;
    }

    public Task<HypercareIncident?> GetIncidentAsync(string incidentId, CancellationToken ct = default)
    {
        return Task.FromResult(_incidents.TryGetValue(incidentId, out var incident) ? incident : null);
    }

    public Task<List<HypercareIncident>> GetOpenIncidentsAsync(string? cutoverId, IncidentSeverity? severity, CancellationToken ct = default)
    {
        var incidents = _incidents.Values.AsEnumerable();

        if (cutoverId != null)
        {
            incidents = incidents.Where(i => i.CutoverId == cutoverId);
        }

        if (severity.HasValue)
        {
            incidents = incidents.Where(i => i.Severity == severity.Value);
        }

        incidents = incidents.Where(i => i.Status != HypercareIncidentStatus.Resolved && i.Status != HypercareIncidentStatus.Closed);

        return Task.FromResult(incidents.OrderByDescending(i => i.ReportedAt).ToList());
    }

    public Task<List<HypercareIncident>> GetIncidentsAsync(string cutoverId, CancellationToken ct = default)
    {
        return Task.FromResult(_incidents.Values.Where(i => i.CutoverId == cutoverId).ToList());
    }

    // Seed default data
    public void SeedDefaultData()
    {
        // Seed readiness criteria
        _readinessCriteria["r1"] = new ReadinessCriteria
        {
            Module = "Identity",
            Criterion = "All roles configured",
            Description = "All roles must be configured and assigned",
            IsCritical = true
        };

        _readinessCriteria["r2"] = new ReadinessCriteria
        {
            Module = "Ingestion",
            Criterion = "OT connectors tested",
            Description = "All OT connectors must be tested and operational",
            IsCritical = true
        };

        _readinessCriteria["r3"] = new ReadinessCriteria
        {
            Module = "Storage",
            Criterion = "Time-series storage ready",
            Description = "Time-series storage must be configured and tested",
            IsCritical = true
        };

        // Seed sample feature flags
        _featureFlags["new-allocation"] = new FeatureFlag
        {
            Id = "new-allocation",
            Name = "New Allocation Engine",
            Module = "Allocation",
            IsEnabled = false,
            Strategy = FeatureFlagStrategy.Progressive,
            IsRisky = true,
            Description = "New allocation engine with improved accuracy"
        };

        _featureFlags["new-optimization"] = new FeatureFlag
        {
            Id = "new-optimization",
            Name = "New Optimization Service",
            Module = "Optimization",
            IsEnabled = false,
            Strategy = FeatureFlagStrategy.TenantBased,
            IsRisky = true,
            Description = "New optimization service with ML improvements"
        };
    }
}


