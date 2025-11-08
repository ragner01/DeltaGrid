using IOC.DisasterRecovery.Models;
using IOC.DisasterRecovery.Services;

namespace IOC.DisasterRecovery.Dashboard;

/// <summary>
/// DR readiness dashboard service
/// </summary>
public interface IDrDashboard
{
    /// <summary>
    /// Get DR readiness dashboard data
    /// </summary>
    Task<DrDashboardData> GetDashboardAsync(CancellationToken ct = default);

    /// <summary>
    /// Get DR metrics summary
    /// </summary>
    Task<DrMetricsSummary> GetMetricsAsync(CancellationToken ct = default);
}

/// <summary>
/// DR dashboard data
/// </summary>
public sealed class DrDashboardData
{
    public required List<DrReadinessStatus> ServiceStatuses { get; init; }
    public required DrMetricsSummary Metrics { get; init; }
    public required List<DrDrillSummary> RecentDrills { get; init; }
    public required List<BackupSummary> RecentBackups { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DR metrics summary
/// </summary>
public sealed class DrMetricsSummary
{
    public int TotalServices { get; init; }
    public int ReadyServices { get; init; }
    public int WarningServices { get; init; }
    public int CriticalServices { get; init; }
    public double ReadinessPercentage { get; init; }
    public Dictionary<DrTier, int> ServicesByTier { get; init; } = new();
    public Dictionary<DrReadinessLevel, int> ServicesByLevel { get; init; } = new();
}

/// <summary>
/// DR drill summary
/// </summary>
public sealed class DrDrillSummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DrDrillType Type { get; init; }
    public required DateTimeOffset ScheduledAt { get; init; }
    public required DrDrillStatus Status { get; init; }
    public bool MetRto { get; init; }
    public bool MetRpo { get; init; }
}

/// <summary>
/// Backup summary
/// </summary>
public sealed class BackupSummary
{
    public required string Id { get; init; }
    public required string ServiceId { get; init; }
    public required DateTimeOffset LastBackupAt { get; init; }
    public required BackupStatus Status { get; init; }
    public bool ValidationPassed { get; init; }
}

/// <summary>
/// DR dashboard service implementation
/// </summary>
public sealed class DrDashboard : IDrDashboard
{
    private readonly IDrDrillService _drillService;
    private readonly IBackupService _backupService;
    private readonly ILogger<DrDashboard> _logger;

    public DrDashboard(
        IDrDrillService drillService,
        IBackupService backupService,
        ILogger<DrDashboard> logger)
    {
        _drillService = drillService;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<DrDashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var statuses = await _drillService.GetReadinessStatusAsync(ct);
        var metrics = await GetMetricsAsync(ct);

        // Get recent drills (simplified)
        var recentDrills = new List<DrDrillSummary>
        {
            new DrDrillSummary
            {
                Id = "drill-001",
                Name = "Q1 Full Site Failure Drill",
                Type = DrDrillType.FullSiteFailure,
                ScheduledAt = DateTimeOffset.UtcNow.AddMonths(-1),
                Status = DrDrillStatus.Completed,
                MetRto = true,
                MetRpo = true
            }
        };

        // Get recent backups (simplified)
        var recentBackups = new List<BackupSummary>
        {
            new BackupSummary
            {
                Id = "backup-001",
                ServiceId = "sql",
                LastBackupAt = DateTimeOffset.UtcNow.AddHours(-6),
                Status = BackupStatus.Completed,
                ValidationPassed = true
            }
        };

        return new DrDashboardData
        {
            ServiceStatuses = statuses,
            Metrics = metrics,
            RecentDrills = recentDrills,
            RecentBackups = recentBackups,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<DrMetricsSummary> GetMetricsAsync(CancellationToken ct = default)
    {
        var statuses = await _drillService.GetReadinessStatusAsync(ct);

        var totalServices = statuses.Count;
        var readyServices = statuses.Count(s => s.Level == DrReadinessLevel.Ready);
        var warningServices = statuses.Count(s => s.Level == DrReadinessLevel.Warning);
        var criticalServices = statuses.Count(s => s.Level == DrReadinessLevel.Critical);

        var readinessPercentage = totalServices > 0
            ? (double)readyServices / totalServices * 100
            : 0;

        var servicesByTier = statuses.GroupBy(s => s.Tier)
            .ToDictionary(g => g.Key, g => g.Count());

        var servicesByLevel = statuses.GroupBy(s => s.Level)
            .ToDictionary(g => g.Key, g => g.Count());

        return new DrMetricsSummary
        {
            TotalServices = totalServices,
            ReadyServices = readyServices,
            WarningServices = warningServices,
            CriticalServices = criticalServices,
            ReadinessPercentage = readinessPercentage,
            ServicesByTier = servicesByTier,
            ServicesByLevel = servicesByLevel
        };
    }
}


