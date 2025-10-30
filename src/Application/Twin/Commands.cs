using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Twin;

namespace IOC.Application.Twin;

public interface ITwinRepository
{
    Task<int> GetCurrentVersionAsync(CancellationToken ct);
    Task<int> BumpVersionAsync(CancellationToken ct);
    Task UpsertNodeAsync(TwinNode node, CancellationToken ct);
    Task<TwinNode?> GetNodeAsync(string idPath, CancellationToken ct);
    Task<IEnumerable<TwinNode>> GetChildrenAsync(string parentPath, CancellationToken ct);
    Task AddEdgeAsync(TwinEdge edge, CancellationToken ct);
    Task<IEnumerable<TwinEdge>> GetEdgesFromAsync(string idPath, CancellationToken ct);
    Task SoftDeleteAsync(string idPath, int newVersion, CancellationToken ct);
}

public interface IKpiProvider
{
    Task<Dictionary<string, double>> GetKpisAsync(string idPath, CancellationToken ct);
}

public static class Commands
{
    public sealed record ImportHierarchyCommand(IReadOnlyList<string> CsvLines) : IRequest<Result<int>>; // returns new version
    public sealed record UpsertNodeCommand(string IdPath, TwinLevel Level, string Name, Dictionary<string, string>? Metadata) : IRequest<Result>;
    public sealed record SoftDeleteNodeCommand(string IdPath) : IRequest<Result>;
    public sealed record SnapshotQuery(string IdPath) : IRequest<Result<object>>;
    public sealed record ImpactQuery(string IdPath, string Relation) : IRequest<Result<IEnumerable<string>>>;

    public sealed class Handlers :
        IRequestHandler<ImportHierarchyCommand, Result<int>>,
        IRequestHandler<UpsertNodeCommand, Result>,
        IRequestHandler<SoftDeleteNodeCommand, Result>,
        IRequestHandler<SnapshotQuery, Result<object>>,
        IRequestHandler<ImpactQuery, Result<IEnumerable<string>>>
    {
        private readonly ITwinRepository _repo;
        private readonly IKpiProvider _kpi;
        public Handlers(ITwinRepository repo, IKpiProvider kpi) { _repo = repo; _kpi = kpi; }

        public async Task<Result<int>> Handle(ImportHierarchyCommand request, CancellationToken cancellationToken)
        {
            var newVersion = await _repo.BumpVersionAsync(cancellationToken);
            foreach (var line in request.CsvLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                // CSV: IdPath,Level,Name
                var parts = line.Split(',');
                if (parts.Length < 3) continue;
                var idPath = parts[0].Trim();
                var level = Enum.Parse<TwinLevel>(parts[1].Trim(), true);
                var name = parts[2].Trim();

                // Path invariants
                if (!idPath.StartsWith('/')) return Result<int>.Failure($"Invalid path: {idPath}");
                var node = new TwinNode(Guid.NewGuid(), idPath, level, name, newVersion);
                await _repo.UpsertNodeAsync(node, cancellationToken);

                // Add parent-child edge if applicable
                var lastSlash = idPath.LastIndexOf('/');
                if (lastSlash > 0)
                {
                    var parent = idPath[..lastSlash];
                    if (!string.IsNullOrWhiteSpace(parent))
                    {
                        await _repo.AddEdgeAsync(new TwinEdge(parent, idPath, "contains", newVersion), cancellationToken);
                    }
                }
            }
            return Result<int>.Success(newVersion);
        }

        public async Task<Result> Handle(UpsertNodeCommand request, CancellationToken cancellationToken)
        {
            var ver = await _repo.BumpVersionAsync(cancellationToken);
            var node = new TwinNode(Guid.NewGuid(), request.IdPath, request.Level, request.Name, ver);
            if (request.Metadata is not null)
            {
                foreach (var kv in request.Metadata)
                {
                    node.SetMetadata(kv.Key, kv.Value);
                }
            }
            await _repo.UpsertNodeAsync(node, cancellationToken);
            return Result.Success();
        }

        public async Task<Result> Handle(SoftDeleteNodeCommand request, CancellationToken cancellationToken)
        {
            var ver = await _repo.BumpVersionAsync(cancellationToken);
            await _repo.SoftDeleteAsync(request.IdPath, ver, cancellationToken);
            return Result.Success();
        }

        public async Task<Result<object>> Handle(SnapshotQuery request, CancellationToken cancellationToken)
        {
            var node = await _repo.GetNodeAsync(request.IdPath, cancellationToken);
            if (node is null || node.IsDeleted) return Result<object>.Failure("Not found");
            var children = await _repo.GetChildrenAsync(request.IdPath, cancellationToken);
            var kpis = await _kpi.GetKpisAsync(request.IdPath, cancellationToken);
            var snapshot = new
            {
                node.IdPath,
                node.Level,
                node.Name,
                node.TopologyVersion,
                Metadata = node.Metadata,
                KPIs = kpis,
                Children = children.Select(c => new { c.IdPath, c.Level, c.Name })
            };
            return Result<object>.Success(snapshot);
        }

        public async Task<Result<IEnumerable<string>>> Handle(ImpactQuery request, CancellationToken cancellationToken)
        {
            var edges = await _repo.GetEdgesFromAsync(request.IdPath, cancellationToken);
            var impacted = new List<string>();
            var q = new Queue<string>();
            q.Enqueue(request.IdPath);
            while (q.Count > 0)
            {
                var current = q.Dequeue();
                var outEdges = await _repo.GetEdgesFromAsync(current, cancellationToken);
                foreach (var e in outEdges.Where(e => e.Relation == request.Relation))
                {
                    impacted.Add(e.ToIdPath);
                    q.Enqueue(e.ToIdPath);
                }
            }
            return Result<IEnumerable<string>>.Success(impacted.Distinct());
        }
    }
}
