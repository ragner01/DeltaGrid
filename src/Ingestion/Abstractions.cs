namespace IOC.Ingestion;

public interface IConnector : IAsyncDisposable
{
    string Name { get; }
    IAsyncEnumerable<TagReading> ReadAsync(CancellationToken cancellationToken);
}

public sealed record TagReading(
    string TagId,
    double Value,
    string Quality, // e.g., Good/Bad/Uncertain
    DateTimeOffset TsSource,
    string Unit,
    string SiteId,
    string AssetId);

public sealed record IngestionEnvelope(
    string TagId,
    double Value,
    string Quality,
    DateTimeOffset TsSource,
    DateTimeOffset TsIngested,
    string Unit,
    string Site,
    string Asset);

public interface IPublisher
{
    Task PublishBatchAsync(IEnumerable<IngestionEnvelope> batch, CancellationToken cancellationToken);
}
