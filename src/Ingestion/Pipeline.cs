using System.Collections.Concurrent;

namespace IOC.Ingestion;

public sealed class Pipeline
{
    private readonly TagRegistry _registry;
    private readonly IPublisher _publisher;
    private readonly ConcurrentDictionary<string, double> _lastValues = new();

    public Pipeline(TagRegistry registry, IPublisher publisher)
    {
        _registry = registry;
        _publisher = publisher;
    }

    public async Task RunAsync(IEnumerable<IConnector> connectors, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<IngestionEnvelope>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });

        // Writer tasks per connector
        var writers = connectors.Select(conn => Task.Run(async () =>
        {
            await foreach (var r in conn.ReadAsync(ct))
            {
                if (!IsGoodQuality(r)) continue;
                if (_registry.TryGet(r.TagId, out var def))
                {
                    var value = Normalize(r.Value, def);
                    if (IsDeadbanded(r.TagId, value, def)) continue;
                    var env = new IngestionEnvelope(r.TagId, value, r.Quality, r.TsSource, DateTimeOffset.UtcNow, def.Unit, r.SiteId, r.AssetId);
                    await channel.Writer.WriteAsync(env, ct);
                }
            }
        }, ct)).ToArray();

        // Batching reader
        var reader = Task.Run(async () =>
        {
            var batch = new List<IngestionEnvelope>(1000);
            var flushInterval = TimeSpan.FromMilliseconds(500);
            var nextFlush = DateTime.UtcNow + flushInterval;
            while (!ct.IsCancellationRequested)
            {
                while (channel.Reader.TryRead(out var item))
                {
                    batch.Add(item);
                    if (batch.Count >= 1000) { await FlushAsync(batch, ct); nextFlush = DateTime.UtcNow + flushInterval; }
                }

                if (DateTime.UtcNow >= nextFlush && batch.Count > 0)
                {
                    await FlushAsync(batch, ct);
                    nextFlush = DateTime.UtcNow + flushInterval;
                }

                await Task.Delay(50, ct);
            }
        }, ct);

        await Task.WhenAll(writers.Concat(new[] { reader }));
    }

    private bool IsGoodQuality(TagReading r) => string.Equals(r.Quality, "Good", StringComparison.OrdinalIgnoreCase);

    private double Normalize(double value, TagDefinition def)
    {
        var scaled = (value * (def.ScaleFactor ?? 1.0)) + (def.ScaleOffset ?? 0.0);
        return scaled;
    }

    private bool IsDeadbanded(string tagId, double value, TagDefinition def)
    {
        var db = def.Deadband ?? 0.0;
        var last = _lastValues.GetOrAdd(tagId, value);
        if (Math.Abs(value - last) < db) return true;
        _lastValues[tagId] = value;
        return false;
    }

    private async Task FlushAsync(List<IngestionEnvelope> batch, CancellationToken ct)
    {
        try
        {
            await _publisher.PublishBatchAsync(batch.ToArray(), ct);
        }
        finally
        {
            batch.Clear();
        }
    }
}
