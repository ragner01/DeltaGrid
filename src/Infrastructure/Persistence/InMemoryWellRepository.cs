using System.Collections.Concurrent;
using IOC.Application.Common.Outbox;
using IOC.Application.Well.AdjustChoke;
using IOC.Core.Domain.Well;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryWellRepository : IWellRepository
{
    private static readonly ConcurrentDictionary<Guid, Well> Store = new();
    private static readonly ConcurrentDictionary<Guid, List<object>> EventStream = new();

    public Task<Well?> GetAsync(Guid id, CancellationToken ct)
    {
        Store.TryGetValue(id, out var well);
        return Task.FromResult(well);
    }

    public Task SaveAsync(Well well, CancellationToken ct)
    {
        Store[well.Id] = well;
        var list = EventStream.GetOrAdd(well.Id, _ => new List<object>());
        list.AddRange(well.DomainEvents);
        return Task.CompletedTask;
    }

    public static Well Seed(string name, LiftMethod lift, Limits limits)
    {
        var w = Well.Create(name, lift, limits);
        Store[w.Id] = w;
        return w;
    }
}

public sealed class InMemoryOutboxStore : IOutboxStore
{
    private static readonly ConcurrentQueue<object> Queue = new();

    public Task EnqueueAsync(object domainEvent, CancellationToken cancellationToken)
    {
        Queue.Enqueue(domainEvent);
        return Task.CompletedTask;
    }
}
