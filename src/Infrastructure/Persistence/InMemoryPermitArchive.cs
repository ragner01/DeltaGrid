using System.Collections.Concurrent;
using IOC.Application.PTW;

namespace IOC.Infrastructure.Persistence;

public sealed class InMemoryPermitArchive : IPermitArchive
{
    private static readonly ConcurrentDictionary<Guid, List<PermitArchiveRecord>> Records = new();

    public Task AppendAsync(PermitArchiveRecord record, CancellationToken ct)
    {
        var list = Records.GetOrAdd(record.PermitId, _ => new List<PermitArchiveRecord>());
        list.Add(record);
        return Task.CompletedTask;
    }

    public static IReadOnlyList<PermitArchiveRecord> Get(Guid permitId)
    {
        return Records.TryGetValue(permitId, out var list) ? list.AsReadOnly() : Array.Empty<PermitArchiveRecord>();
    }
}
