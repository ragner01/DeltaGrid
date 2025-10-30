using System.Collections.Concurrent;
using IOC.Application.PTW;
using IOC.Core.Domain.PTW;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryPtwRepository : IPtwRepository
{
    private static readonly ConcurrentDictionary<Guid, WorkOrder> WorkOrders = new();
    private static readonly ConcurrentDictionary<Guid, Permit> Permits = new();

    public Task<WorkOrder?> GetWorkOrderAsync(Guid id, CancellationToken ct)
    {
        WorkOrders.TryGetValue(id, out var wo);
        return Task.FromResult(wo);
    }

    public Task<Permit?> GetPermitAsync(Guid id, CancellationToken ct)
    {
        Permits.TryGetValue(id, out var p);
        return Task.FromResult(p);
    }

    public Task SaveWorkOrderAsync(WorkOrder wo, CancellationToken ct)
    {
        WorkOrders[wo.Id] = wo;
        return Task.CompletedTask;
    }

    public Task SavePermitAsync(Permit p, CancellationToken ct)
    {
        Permits[p.Id] = p;
        return Task.CompletedTask;
    }
}
