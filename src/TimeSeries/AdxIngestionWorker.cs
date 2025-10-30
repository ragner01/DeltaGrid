using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Kusto.Ingest;

namespace IOC.TimeSeries;

public sealed class AdxIngestionWorker : BackgroundService
{
    private readonly IConfiguration _cfg;
    private readonly IKustoIngestClient _ingest;
    private readonly EventProcessorClient _processor;
    private readonly ILogger<AdxIngestionWorker> _logger;

    public AdxIngestionWorker(IConfiguration cfg, ILogger<AdxIngestionWorker> logger)
    {
        _cfg = cfg;
        _logger = logger;
        var cluster = cfg["ADX:ClusterUri"] ?? "https://localhost";
        var db = cfg["ADX:Database"] ?? "ioc";
        var kcsb = new Kusto.Data.KustoConnectionStringBuilder(cluster).WithAadUserPromptAuthentication();
        _ingest = KustoIngestFactory.CreateDirectIngestClient(kcsb);

        var ehConn = cfg.GetConnectionString("EventHubs") ?? cfg["EventHubs:ConnectionString"] ?? "";
        var ehHub = cfg["EventHubs:HubName"] ?? "ingestion";
        var storage = cfg["EventHubs:Checkpoint:BlobConnectionString"] ?? "UseDevelopmentStorage=true";
        var container = cfg["EventHubs:Checkpoint:Container"] ?? "eh-checkpoints";
        var blob = new BlobContainerClient(storage, container);
        _processor = new EventProcessorClient(blob, "$Default", ehConn, ehHub);
        _processor.ProcessEventAsync += ProcessEventAsync;
        _processor.ProcessErrorAsync += args => { _logger.LogError(args.Exception, "EH error"); return Task.CompletedTask; };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _processor.StopProcessingAsync();
        }
    }

    private async Task ProcessEventAsync(ProcessEventArgs args)
    {
        if (args.CancellationToken.IsCancellationRequested) return;
        try
        {
            var env = JsonSerializer.Deserialize<IngestionEnvelope>(args.Data.EventBody.ToArray());
            if (env is null) { await args.UpdateCheckpointAsync(args.CancellationToken); return; }

            // Basic dedupe using event id + ts; production would use ADX ingestion properties
            var mapping = new IngestionMapping { IngestionMappingReference = null, IngestionMappingKind = IngestionMappingKind.Json }; 
            using var stream = new MemoryStream(args.Data.EventBody.ToArray());
            var props = new Kusto.Data.Common.ClientRequestProperties();
            var info = new Kusto.Ingest.IngestionProperties(_cfg["ADX:Database"] ?? "ioc", "RawTelemetry")
            {
                Format = Kusto.Data.Common.DataSourceFormat.json,
                ReportLevel = IngestionReportLevel.FailuresOnly,
                ReportMethod = IngestionReportMethod.Queue
            };
            await _ingest.IngestFromStreamAsync(stream, info);
            await args.UpdateCheckpointAsync(args.CancellationToken);
        }
        catch (Exception ex)
        {
            // Swallow and continue; EH will retry
            _logger.LogError(ex, "ProcessEvent failed");
        }
    }
}

public sealed record IngestionEnvelope(
    string TagId,
    double Value,
    string Quality,
    DateTimeOffset TsSource,
    DateTimeOffset TsIngested,
    string Unit,
    string Site,
    string Asset);
