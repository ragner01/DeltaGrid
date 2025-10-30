using SQLite;

namespace FieldApp.Sync;

public sealed class SQLiteStore : ILocalStore
{
    private readonly SQLiteAsyncConnection _db;
    public SQLiteStore(string path)
    {
        _db = new SQLiteAsyncConnection(path);
    }

    public async Task InitializeAsync()
    {
        await _db.CreateTableAsync<VectorClock>();
    }

    public Task<IReadOnlyList<VectorClock>> GetClocksAsync() => _db.Table<VectorClock>().ToListAsync().ContinueWith(t => (IReadOnlyList<VectorClock>)t.Result);

    public async Task SetClockAsync(VectorClock clock)
    {
        var existing = await _db.Table<VectorClock>().Where(c => c.EntityType == clock.EntityType && c.EntityId == clock.EntityId).FirstOrDefaultAsync();
        if (existing is null) await _db.InsertAsync(clock); else { existing.Version = Math.Max(existing.Version, clock.Version); await _db.UpdateAsync(existing); }
    }

    public Task UpsertAsync<T>(T entity) where T : class, ISyncEntity, new() => _db.InsertOrReplaceAsync(entity);

    public Task<IReadOnlyList<T>> GetDirtyAsync<T>() where T : class, ISyncEntity, new()
    {
        // Placeholder: track dirty via a convention or separate table; return empty for now
        return Task.FromResult((IReadOnlyList<T>)Array.Empty<T>());
    }

    public Task MarkCleanAsync<T>(T entity) where T : class, ISyncEntity, new() => Task.CompletedTask;
}
