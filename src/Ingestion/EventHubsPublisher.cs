using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace IOC.Ingestion;

public sealed class EventHubsPublisher : IPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producer;

    public EventHubsPublisher(IConfiguration config)
    {
        var cs = config.GetConnectionString("EventHubs") ?? config["EventHubs:ConnectionString"] ?? "";
        var hub = config["EventHubs:HubName"] ?? "ingestion";
        _producer = new EventHubProducerClient(cs, hub);
    }

    public async Task PublishBatchAsync(IEnumerable<IngestionEnvelope> batch, CancellationToken cancellationToken)
    {
        using var eb = await _producer.CreateBatchAsync(cancellationToken);
        foreach (var e in batch)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(e);
            if (!eb.TryAdd(new EventData(payload)))
            {
                await _producer.SendAsync(eb, cancellationToken);
                using var eb2 = await _producer.CreateBatchAsync(cancellationToken);
                if (!eb2.TryAdd(new EventData(payload)))
                {
                    // Oversized single event; drop with log in real system
                    continue;
                }
                await _producer.SendAsync(eb2, cancellationToken);
                return;
            }
        }
        if (eb.Count > 0)
        {
            await _producer.SendAsync(eb, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync() => await _producer.DisposeAsync();
}
