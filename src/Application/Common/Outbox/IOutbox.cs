namespace IOC.Application.Common.Outbox;

public interface IOutboxPublisher
{
    Task PublishAsync(object domainEvent, CancellationToken cancellationToken);
}

public interface IOutboxStore
{
    Task EnqueueAsync(object domainEvent, CancellationToken cancellationToken);
}
