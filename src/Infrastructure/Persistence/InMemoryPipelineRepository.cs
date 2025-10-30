using System.Collections.Concurrent;
using IOC.Application.Pipeline;
using IOC.Core.Domain.Pipeline;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryPipelineRepository : IPipelineRepository
{
    private static readonly ConcurrentDictionary<string, SegmentBaseline> Baselines = new();
    private static readonly ConcurrentDictionary<Guid, LeakIncident> Incidents = new();

    public Task SaveBaselineAsync(SegmentBaseline baseline, CancellationToken ct)
    {
        Baselines[baseline.SegmentId] = baseline; return Task.CompletedTask;
    }

    public Task<SegmentBaseline?> GetBaselineAsync(string segmentId, CancellationToken ct)
    {
        Baselines.TryGetValue(segmentId, out var b); return Task.FromResult(b);
    }

    public Task SaveIncidentAsync(LeakIncident incident, CancellationToken ct)
    {
        Incidents[incident.Id] = incident; return Task.CompletedTask;
    }
}
