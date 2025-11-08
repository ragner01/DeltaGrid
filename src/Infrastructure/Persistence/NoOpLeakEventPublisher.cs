using IOC.Application.Pipeline;
using IOC.Core.Domain.Pipeline;

using Microsoft.Extensions.Logging;

namespace IOC.Infrastructure.Persistence;

public sealed class NoOpLeakEventPublisher : ILeakEventPublisher
{
    private readonly ILogger<NoOpLeakEventPublisher> _logger;
    public NoOpLeakEventPublisher(ILogger<NoOpLeakEventPublisher> logger) { _logger = logger; }
    public Task PublishLeakAsync(LeakIncident incident, CancellationToken ct)
    {
        _logger.LogInformation("Leak incident published (no-op): {id} seg={seg} conf={conf}", incident.Id, incident.SegmentId, incident.Confidence);
        return Task.CompletedTask;
    }
}
