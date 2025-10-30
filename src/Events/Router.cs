using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace IOC.Events;

public interface INotificationSink
{
    Task NotifyAsync(CanonicalEvent e, CancellationToken ct);
}

public sealed class WebhookNotificationSink : INotificationSink
{
    private readonly HttpClient _http = new();
    private readonly string _url;
    public WebhookNotificationSink(string url) { _url = url; }
    public async Task NotifyAsync(CanonicalEvent e, CancellationToken ct)
    {
        try { await _http.PostAsJsonAsync(_url, e, ct); } catch { }
    }
}

public sealed class EventStore
{
    private readonly ConcurrentDictionary<Guid, CanonicalEvent> _events = new();
    private readonly List<(Guid, DateTimeOffset)> _acks = new();

    public CanonicalEvent Save(CanonicalEvent e)
    {
        _events[e.Id] = e; return e;
    }

    public bool TryGet(Guid id, out CanonicalEvent e) => _events.TryGetValue(id, out e!);
    public IEnumerable<CanonicalEvent> All() => _events.Values.OrderByDescending(e => e.OccurredAt);
    public void Ack(Guid id, DateTimeOffset when) { _acks.Add((id, when)); }
}

public sealed class SuppressionPolicy
{
    public TimeSpan DedupWindow { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan ChatterWindow { get; init; } = TimeSpan.FromSeconds(30);
    public int FloodThreshold { get; init; } = 50;
    public bool MaintenanceMode { get; init; } = false;
}

public sealed class Router
{
    private readonly EventStore _store;
    private readonly INotificationSink _notify;
    private readonly SuppressionPolicy _policy;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastByFingerprint = new();
    private readonly ConcurrentQueue<DateTimeOffset> _recent = new();
    private readonly Meter _meter = new("IOC.Events", "1.0.0");
    private readonly Counter<int> _routed;

    public Router(EventStore store, INotificationSink notify, SuppressionPolicy policy)
    {
        _store = store; _notify = notify; _policy = policy; _routed = _meter.CreateCounter<int>("events_routed");
    }

    public async Task<CanonicalEvent?> RouteAsync(string tenant, string site, string asset, RawAlarm raw, Severity sev, Consequence cons, string category, int priority, bool shelved, CancellationToken ct)
    {
        if (_policy.MaintenanceMode) return null;

        string fp = Fingerprint(raw);
        var now = DateTimeOffset.UtcNow;
        if (_lastByFingerprint.TryGetValue(fp, out var last) && now - last < _policy.DedupWindow)
        {
            return null; // dedup
        }
        _lastByFingerprint[fp] = now;

        // flood control
        _recent.Enqueue(now);
        while (_recent.TryPeek(out var head) && now - head > TimeSpan.FromSeconds(5)) _recent.TryDequeue(out _);
        if (_recent.Count > _policy.FloodThreshold) return null;

        var e = new CanonicalEvent(Guid.NewGuid(), tenant, site, asset, sev, cons, category, raw.Message, raw.Source, raw.TagId, raw.OccurredAt, now, priority, fp, shelved, "New");
        _store.Save(e);
        _routed.Add(1);
        if (!shelved) await _notify.NotifyAsync(e, ct);
        return e;
    }

    private static string Fingerprint(RawAlarm raw)
    {
        return $"{raw.Source}|{raw.TagId}|{raw.Code}|{raw.Message}".ToLowerInvariant();
    }
}
