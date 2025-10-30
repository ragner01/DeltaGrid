using FieldApp.Sync;

namespace FieldApp.Twin;

public sealed class LocalTwinCache
{
    private readonly ILocalStore _store;
    public LocalTwinCache(ILocalStore store) { _store = store; }

    public sealed class TwinNodeDto : ISyncEntity
    {
        public string Id { get; set; } = string.Empty;
        public long Version { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string IdPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }

    public Task UpsertAsync(TwinNodeDto node) => _store.UpsertAsync(node);
}
