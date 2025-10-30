using System.Collections.Concurrent;
using IOC.Application.Twin;
using IOC.Core.Domain.Twin;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryTwinRepository : ITwinRepository
{
    private static int Version = 1;
    private static readonly ConcurrentDictionary<string, TwinNode> Nodes = new();
    private static readonly ConcurrentDictionary<string, List<TwinEdge>> OutEdges = new();

    public Task<int> GetCurrentVersionAsync(CancellationToken ct) => Task.FromResult(Version);
    public Task<int> BumpVersionAsync(CancellationToken ct) { Version++; return Task.FromResult(Version); }

    public Task UpsertNodeAsync(TwinNode node, CancellationToken ct)
    {
        Nodes[node.IdPath] = node; return Task.CompletedTask;
    }

    public Task<TwinNode?> GetNodeAsync(string idPath, CancellationToken ct)
    {
        Nodes.TryGetValue(idPath, out var n); return Task.FromResult(n);
    }

    public Task<IEnumerable<TwinNode>> GetChildrenAsync(string parentPath, CancellationToken ct)
    {
        if (OutEdges.TryGetValue(parentPath, out var list))
        {
            var children = list.Select(e => e.ToIdPath).Where(Nodes.ContainsKey).Select(p => Nodes[p]).Where(n => !n.IsDeleted);
            return Task.FromResult(children);
        }
        return Task.FromResult(Enumerable.Empty<TwinNode>());
    }

    public Task AddEdgeAsync(TwinEdge edge, CancellationToken ct)
    {
        OutEdges.AddOrUpdate(edge.FromIdPath, _ => new List<TwinEdge> { edge }, (_, lst) => { lst.Add(edge); return lst; });
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TwinEdge>> GetEdgesFromAsync(string idPath, CancellationToken ct)
    {
        OutEdges.TryGetValue(idPath, out var list); return Task.FromResult<IEnumerable<TwinEdge>>(list ?? new List<TwinEdge>());
    }

    public Task SoftDeleteAsync(string idPath, int newVersion, CancellationToken ct)
    {
        if (Nodes.TryGetValue(idPath, out var n))
        {
            n.MarkDeleted(newVersion);
        }
        return Task.CompletedTask;
    }
}

public sealed class NoOpKpiProvider : IKpiProvider
{
    public Task<Dictionary<string, double>> GetKpisAsync(string idPath, CancellationToken ct)
    {
        // Demo KPIs
        var rnd = new Random(idPath.GetHashCode());
        return Task.FromResult(new Dictionary<string, double>
        {
            ["Availability"] = Math.Round(90 + rnd.NextDouble() * 10, 2),
            ["Throughput"] = Math.Round(1000 + rnd.NextDouble() * 200, 2),
            ["EnergyIntensity"] = Math.Round(0.8 + rnd.NextDouble() * 0.3, 3)
        });
    }
}
