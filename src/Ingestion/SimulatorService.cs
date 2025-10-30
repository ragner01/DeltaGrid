namespace IOC.Ingestion;

public sealed class SimulatorService : BackgroundService
{
    private readonly TagRegistry _registry;
    private readonly IPublisher _publisher;
    private readonly ILogger<SimulatorService> _logger;

    public SimulatorService(TagRegistry registry, IPublisher publisher, ILogger<SimulatorService> logger)
    {
        _registry = registry;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipeline = new Pipeline(_registry, _publisher);
        var connectors = new IConnector[]
        {
            new OpcUaConnector(), new MqttConnector(), new PiConnector()
        };

        _logger.LogInformation("Starting ingestion simulator with {count} connectors", connectors.Length);
        await pipeline.RunAsync(connectors, stoppingToken);
    }
}
