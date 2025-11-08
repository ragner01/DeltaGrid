using IOC.Search.Indexing;

namespace IOC.Search.Indexing;

/// <summary>
/// Indexing pipelines for different document sources
/// </summary>
public interface IIndexingPipeline
{
    Task IndexFromSourceAsync(CancellationToken ct = default);
}

/// <summary>
/// Indexes SOPs from file system or CMS
/// </summary>
public sealed class SopIndexingPipeline : IIndexingPipeline
{
    private readonly IDocumentIndexer _indexer;
    private readonly ILogger<SopIndexingPipeline> _logger;

    public SopIndexingPipeline(IDocumentIndexer indexer, ILogger<SopIndexingPipeline> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task IndexFromSourceAsync(CancellationToken ct = default)
    {
        // In production, read from CMS, SharePoint, or file system
        _logger.LogInformation("SOP indexing pipeline triggered");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Indexes permits from PTW repository
/// </summary>
public sealed class PermitIndexingPipeline : IIndexingPipeline
{
    private readonly IDocumentIndexer _indexer;
    private readonly ILogger<PermitIndexingPipeline> _logger;

    public PermitIndexingPipeline(IDocumentIndexer indexer, ILogger<PermitIndexingPipeline> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task IndexFromSourceAsync(CancellationToken ct = default)
    {
        // In production, query PTW repository for permits and index
        _logger.LogInformation("Permit indexing pipeline triggered");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Indexes lab results from lab repository
/// </summary>
public sealed class LabResultIndexingPipeline : IIndexingPipeline
{
    private readonly IDocumentIndexer _indexer;
    private readonly ILogger<LabResultIndexingPipeline> _logger;

    public LabResultIndexingPipeline(IDocumentIndexer indexer, ILogger<LabResultIndexingPipeline> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task IndexFromSourceAsync(CancellationToken ct = default)
    {
        // In production, query lab repository for results and index
        _logger.LogInformation("Lab result indexing pipeline triggered");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Indexes incidents from events/alarms repository
/// </summary>
public sealed class IncidentIndexingPipeline : IIndexingPipeline
{
    private readonly IDocumentIndexer _indexer;
    private readonly ILogger<IncidentIndexingPipeline> _logger;

    public IncidentIndexingPipeline(IDocumentIndexer indexer, ILogger<IncidentIndexingPipeline> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }

    public async Task IndexFromSourceAsync(CancellationToken ct = default)
    {
        // In production, query events repository for incidents and index
        _logger.LogInformation("Incident indexing pipeline triggered");
        await Task.CompletedTask;
    }
}

