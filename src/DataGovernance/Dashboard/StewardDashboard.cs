using IOC.DataGovernance.Models;
using IOC.DataGovernance.Services;

namespace IOC.DataGovernance.Dashboard;

/// <summary>
/// Steward dashboard service
/// </summary>
public interface IStewardDashboard
{
    /// <summary>
    /// Get steward dashboard data
    /// </summary>
    Task<StewardDashboardData> GetDashboardAsync(string stewardId, CancellationToken ct = default);

    /// <summary>
    /// Get DQ breach trends
    /// </summary>
    Task<DqBreachTrends> GetBreachTrendsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default);
}

/// <summary>
/// Steward dashboard data
/// </summary>
public sealed class StewardDashboardData
{
    public required List<DqBreach> OpenBreaches { get; init; }
    public required List<DqException> PendingExceptions { get; init; }
    public required List<AccessRequest> PendingRequests { get; init; }
    public required DqBreachStatistics Statistics { get; init; }
    public required Dictionary<string, Dictionary<DqDimension, double>> DatasetScores { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DQ breach trends
/// </summary>
public sealed class DqBreachTrends
{
    public required List<BreachTrendPoint> TrendPoints { get; init; }
    public required Dictionary<DqDimension, int> BreachesByDimension { get; init; }
    public required Dictionary<string, int> TopBreachDatasets { get; init; }
    public required TimeSpan MeanTimeToRemediation { get; init; }
}

/// <summary>
/// Breach trend point
/// </summary>
public sealed class BreachTrendPoint
{
    public required DateTimeOffset Date { get; init; }
    public int BreachCount { get; init; }
    public int ResolvedCount { get; init; }
}

/// <summary>
/// Steward dashboard service implementation
/// </summary>
public sealed class StewardDashboard : IStewardDashboard
{
    private readonly IDqBreachService _breachService;
    private readonly IDqExceptionService _exceptionService;
    private readonly IAccessRequestService _accessRequestService;
    private readonly IDqEngine _dqEngine;
    private readonly IDatasetMetadataRepository _datasetRepository;
    private readonly ILogger<StewardDashboard> _logger;

    public StewardDashboard(
        IDqBreachService breachService,
        IDqExceptionService exceptionService,
        IAccessRequestService accessRequestService,
        IDqEngine dqEngine,
        IDatasetMetadataRepository datasetRepository,
        ILogger<StewardDashboard> logger)
    {
        _breachService = breachService;
        _exceptionService = exceptionService;
        _accessRequestService = accessRequestService;
        _dqEngine = dqEngine;
        _datasetRepository = datasetRepository;
        _logger = logger;
    }

    public async Task<StewardDashboardData> GetDashboardAsync(string stewardId, CancellationToken ct = default)
    {
        // Get datasets owned by steward
        var datasets = await _datasetRepository.GetDatasetsByOwnerAsync(stewardId, ct);
        var datasetIds = datasets.Select(d => d.Id).ToList();

        // Get open breaches for steward's datasets
        var allOpenBreaches = await _breachService.GetOpenBreachesAsync(null, ct);
        var openBreaches = allOpenBreaches
            .Where(b => datasetIds.Contains(b.DatasetId))
            .ToList();

        // Get pending exceptions
        var pendingExceptions = await _exceptionService.GetPendingExceptionsAsync(ct);
        var relevantExceptions = pendingExceptions
            .Where(e => openBreaches.Any(b => b.Id == e.BreachId))
            .ToList();

        // Get pending access requests for steward's datasets
        var pendingRequests = await _accessRequestService.GetPendingRequestsAsync(ct);
        var relevantRequests = pendingRequests
            .Where(r => datasetIds.Contains(r.DatasetId))
            .ToList();

        // Get statistics
        var statistics = await _breachService.GetBreachStatisticsAsync(null, null, ct);

        // Get DQ scores for steward's datasets
        var datasetScores = new Dictionary<string, Dictionary<DqDimension, double>>();
        foreach (var datasetId in datasetIds)
        {
            var scores = await _dqEngine.GetDatasetScoreAsync(datasetId, ct);
            datasetScores[datasetId] = scores;
        }

        return new StewardDashboardData
        {
            OpenBreaches = openBreaches,
            PendingExceptions = relevantExceptions,
            PendingRequests = relevantRequests,
            Statistics = statistics,
            DatasetScores = datasetScores,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<DqBreachTrends> GetBreachTrendsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default)
    {
        var from = fromDate ?? DateTimeOffset.UtcNow.AddDays(-30);
        var to = toDate ?? DateTimeOffset.UtcNow;

        // In production: Query breach repository for trends
        // Placeholder: generate sample trend data
        var trendPoints = new List<BreachTrendPoint>();
        var current = from;
        while (current <= to)
        {
            trendPoints.Add(new BreachTrendPoint
            {
                Date = current,
                BreachCount = Random.Shared.Next(5, 20),
                ResolvedCount = Random.Shared.Next(3, 15)
            });
            current = current.AddDays(1);
        }

        var statistics = await _breachService.GetBreachStatisticsAsync(from, to, ct);

        var topBreachDatasets = statistics.BreachesByDataset
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new DqBreachTrends
        {
            TrendPoints = trendPoints,
            BreachesByDimension = statistics.BreachesByDimension,
            TopBreachDatasets = topBreachDatasets,
            MeanTimeToRemediation = statistics.AverageTimeToResolution
        };
    }
}

