namespace IOC.Ingestion;

public sealed class ConnectorHostService : BackgroundService
{
    private readonly IConfiguration _cfg;
    private readonly TagRegistry _registry;
    private readonly IPublisher _publisher;
    private readonly ILogger<ConnectorHostService> _logger;

    public ConnectorHostService(IConfiguration cfg, TagRegistry registry, IPublisher publisher, ILogger<ConnectorHostService> logger)
    {
        _cfg = cfg;
        _registry = registry;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectors = new List<IConnector>();
        if (_cfg.GetValue("Connectors:OpcUa:Enabled", true)) connectors.Add(new OpcUaConnector(_cfg));
        if (_cfg.GetValue("Connectors:Mqtt:Enabled", true)) connectors.Add(new MqttConnector(_cfg));
        if (_cfg.GetValue("Connectors:Pi:Enabled", false)) connectors.Add(new PiConnector(_cfg));

        _logger.LogInformation("Starting connectors: {names}", string.Join(", ", connectors.Select(c => c.Name)));
        var pipeline = new Pipeline(_registry, _publisher);
        await pipeline.RunAsync(connectors, stoppingToken);
    }
}
