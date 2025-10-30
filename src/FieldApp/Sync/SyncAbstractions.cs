using SQLite;

namespace FieldApp.Sync;

public sealed class VectorClock
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public long Version { get; set; }
}

public interface ISyncEntity
{
    string Id { get; }
    long Version { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}

public sealed record DeltaEnvelope(string EntityType, string EntityId, long Version, byte[] Payload, string ContentType);

public interface ILocalStore
{
    Task InitializeAsync();
    Task<IReadOnlyList<VectorClock>> GetClocksAsync();
    Task SetClockAsync(VectorClock clock);
    Task UpsertAsync<T>(T entity) where T : class, ISyncEntity, new();
    Task<IReadOnlyList<T>> GetDirtyAsync<T>() where T : class, ISyncEntity, new();
    Task MarkCleanAsync<T>(T entity) where T : class, ISyncEntity, new();
}

public interface IRemoteSync
{
    Task<IReadOnlyList<DeltaEnvelope>> PullAsync(IReadOnlyList<VectorClock> clocks, CancellationToken ct);
    Task PushAsync<T>(IReadOnlyList<T> changes, CancellationToken ct) where T : class, ISyncEntity, new();
}

public sealed class SyncEngine
{
    private readonly ILocalStore _store;
    private readonly IRemoteSync _remote;
    public SyncEngine(ILocalStore store, IRemoteSync remote) { _store = store; _remote = remote; }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await _store.InitializeAsync();
        var clocks = await _store.GetClocksAsync();
        var deltas = await _remote.PullAsync(clocks, ct);
        foreach (var d in deltas)
        {
            // Application-specific deserialization; omitted here
            // Update clocks monotonically
            await _store.SetClockAsync(new VectorClock { EntityType = d.EntityType, EntityId = d.EntityId, Version = d.Version });
        }
        // Push local dirty changes
        // Caller will provide typed syncs per entity type
    }
}

public sealed class Backoff
{
    public static async Task RetryAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try { await work(ct); return; }
            catch when (attempt < 4)
            {
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
    }
}
