using IOC.DisasterRecovery.Models;
using IOC.DisasterRecovery.Replay;
using IOC.DisasterRecovery.Services;

namespace IOC.DisasterRecovery.Persistence;

/// <summary>
/// In-memory implementation of DR repositories
/// </summary>
public sealed class InMemoryDrRepository :
    IBackupRepository,
    IFailoverRepository,
    IDrDrillRepository,
    IDrClassificationService,
    IEventReplayRepository
{
    private readonly Dictionary<string, BackupDefinition> _backups = new();
    private readonly Dictionary<string, BackupExecution> _backupExecutions = new();
    private readonly Dictionary<string, RestoreTest> _restoreTests = new();
    private readonly Dictionary<string, FailoverConfiguration> _failovers = new();
    private readonly Dictionary<string, ServiceDrClassification> _classifications = new();
    private readonly Dictionary<string, DrDrill> _drills = new();
    private readonly Dictionary<string, ReplayExecution> _replayExecutions = new();

    // IBackupRepository
    public Task<BackupDefinition?> GetBackupDefinitionAsync(string backupId, CancellationToken ct = default)
    {
        return Task.FromResult(_backups.TryGetValue(backupId, out var backup) ? backup : null);
    }

    public Task SaveExecutionAsync(BackupExecution execution, CancellationToken ct = default)
    {
        _backupExecutions[execution.Id] = execution;
        return Task.CompletedTask;
    }

    public Task<BackupExecution?> GetExecutionAsync(string executionId, CancellationToken ct = default)
    {
        return Task.FromResult(_backupExecutions.TryGetValue(executionId, out var exec) ? exec : null);
    }

    public Task<List<BackupExecution>> GetExecutionHistoryAsync(string backupId, int limit, CancellationToken ct = default)
    {
        return Task.FromResult(_backupExecutions.Values
            .Where(e => e.BackupId == backupId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToList());
    }

    public Task UpdateBackupLastRunAsync(string backupId, DateTimeOffset lastRun, CancellationToken ct = default)
    {
        // In-memory implementation - last run is tracked in execution history
        return Task.CompletedTask;
    }

    public Task SaveRestoreTestAsync(RestoreTest test, CancellationToken ct = default)
    {
        _restoreTests[test.Id] = test;
        return Task.CompletedTask;
    }

    public Task<List<RestoreTest>> GetRestoreTestHistoryAsync(string backupId, CancellationToken ct = default)
    {
        return Task.FromResult(_restoreTests.Values
            .Where(t => t.BackupId == backupId)
            .OrderByDescending(t => t.ScheduledAt)
            .ToList());
    }

    public Task UpdateRestoreTestLastRunAsync(string backupId, DateTimeOffset lastRun, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    // IFailoverRepository
    public Task<FailoverConfiguration?> GetFailoverAsync(string failoverId, CancellationToken ct = default)
    {
        return Task.FromResult(_failovers.TryGetValue(failoverId, out var failover) ? failover : null);
    }

    public Task<FailoverConfiguration?> GetFailoverByServiceAsync(string serviceId, CancellationToken ct = default)
    {
        return Task.FromResult(_failovers.Values.FirstOrDefault(f => f.ServiceId == serviceId));
    }

    public Task UpdateFailoverAsync(string failoverId, DateTimeOffset lastFailover, CancellationToken ct = default)
    {
        if (_failovers.TryGetValue(failoverId, out var failover))
        {
            _failovers[failoverId] = failover with { LastFailoverAt = lastFailover };
        }
        return Task.CompletedTask;
    }

    public Task UpdateFailoverTestAsync(string failoverId, DateTimeOffset lastTest, CancellationToken ct = default)
    {
        if (_failovers.TryGetValue(failoverId, out var failover))
        {
            _failovers[failoverId] = failover with { LastFailoverTestAt = lastTest };
        }
        return Task.CompletedTask;
    }

    // IDrDrillRepository
    public Task SaveDrillAsync(DrDrill drill, CancellationToken ct = default)
    {
        _drills[drill.Id] = drill;
        return Task.CompletedTask;
    }

    public Task<DrDrill?> GetDrillAsync(string drillId, CancellationToken ct = default)
    {
        return Task.FromResult(_drills.TryGetValue(drillId, out var drill) ? drill : null);
    }

    public Task<List<DrDrill>> GetDrillHistoryAsync(int limit = 100, CancellationToken ct = default)
    {
        return Task.FromResult(_drills.Values
            .OrderByDescending(d => d.ScheduledAt)
            .Take(limit)
            .ToList());
    }

    // IDrClassificationService
    public Task<ServiceDrClassification?> GetClassificationAsync(string serviceId, CancellationToken ct = default)
    {
        return Task.FromResult(_classifications.TryGetValue(serviceId, out var classification) ? classification : null);
    }

    public Task<List<ServiceDrClassification>> GetAllClassificationsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_classifications.Values.ToList());
    }

    public Task SaveClassificationAsync(ServiceDrClassification classification, CancellationToken ct = default)
    {
        _classifications[classification.ServiceId] = classification;
        return Task.CompletedTask;
    }

    // IEventReplayRepository
    public Task SaveExecutionAsync(ReplayExecution execution, CancellationToken ct = default)
    {
        _replayExecutions[execution.Id] = execution;
        return Task.CompletedTask;
    }

    public Task<List<ReplayExecution>> GetExecutionHistoryAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_replayExecutions.Values
            .OrderByDescending(e => e.ExecutedAt)
            .ToList());
    }

    // Seed default classifications
    public void SeedClassifications()
    {
        _classifications["api"] = new ServiceDrClassification
        {
            ServiceId = "api",
            ServiceName = "Web API",
            Tier = DrTier.Critical,
            Rto = TimeSpan.FromHours(1),
            Rpo = TimeSpan.FromMinutes(15),
            GeoRedundant = true,
            AutomatedFailover = true
        };

        _classifications["sql"] = new ServiceDrClassification
        {
            ServiceId = "sql",
            ServiceName = "SQL Database",
            Tier = DrTier.Critical,
            Rto = TimeSpan.FromHours(1),
            Rpo = TimeSpan.FromMinutes(15),
            GeoRedundant = true,
            AutomatedFailover = true,
            BackupStrategy = "Full backups every 6 hours, transaction log backups every 15 minutes"
        };

        _classifications["ingestion"] = new ServiceDrClassification
        {
            ServiceId = "ingestion",
            ServiceName = "OT Ingestion",
            Tier = DrTier.High,
            Rto = TimeSpan.FromHours(4),
            Rpo = TimeSpan.FromHours(1),
            GeoRedundant = true,
            AutomatedFailover = false
        };

        _classifications["reporting"] = new ServiceDrClassification
        {
            ServiceId = "reporting",
            ServiceName = "Reporting Service",
            Tier = DrTier.Standard,
            Rto = TimeSpan.FromHours(24),
            Rpo = TimeSpan.FromHours(4),
            GeoRedundant = false,
            AutomatedFailover = false
        };
    }
}


