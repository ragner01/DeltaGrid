using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data quality breach service
/// </summary>
public interface IDqBreachService
{
    /// <summary>
    /// Detect breaches from DQ scores
    /// </summary>
    Task<List<DqBreach>> DetectBreachesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get open breaches
    /// </summary>
    Task<List<DqBreach>> GetOpenBreachesAsync(string? datasetId = null, CancellationToken ct = default);

    /// <summary>
    /// Acknowledge breach
    /// </summary>
    Task AcknowledgeBreachAsync(string breachId, string acknowledgedBy, CancellationToken ct = default);

    /// <summary>
    /// Resolve breach
    /// </summary>
    Task ResolveBreachAsync(string breachId, string resolvedBy, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Get breach statistics
    /// </summary>
    Task<DqBreachStatistics> GetBreachStatisticsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default);
}

/// <summary>
/// DQ breach statistics
/// </summary>
public sealed class DqBreachStatistics
{
    public int TotalBreaches { get; init; }
    public int OpenBreaches { get; init; }
    public int ResolvedBreaches { get; init; }
    public int ExceptionBreaches { get; init; }
    public TimeSpan AverageTimeToResolution { get; init; }
    public Dictionary<DqDimension, int> BreachesByDimension { get; init; } = new();
    public Dictionary<string, int> BreachesByDataset { get; init; } = new();
}


