using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOC.Ingestion;

public sealed class SimulatorService : BackgroundService
{
    private readonly TagRegistry _registry;
    private readonly IPublisher _publisher;
    private readonly ILogger<SimulatorService> _logger;
    private readonly IConfiguration _configuration;

    public SimulatorService(TagRegistry registry, IPublisher publisher, ILogger<SimulatorService> logger, IConfiguration configuration)
    {
        _registry = registry;
        _publisher = publisher;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeline = new Pipeline(_registry, _publisher);
        var connectors = new IConnector[]
        {
            new OpcUaConnector(_configuration),
            new MqttConnector(_configuration),
            new PiConnector(_configuration)
        };

        _logger.LogInformation("Starting ingestion simulator with {count} connectors", connectors.Length);
        await pipeline.RunAsync(connectors, stoppingToken);
    }
}
