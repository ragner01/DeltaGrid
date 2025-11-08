using IOC.DisasterRecovery.Models;

namespace IOC.DisasterRecovery.Services;

/// <summary>
/// Backup service for automated backups and validation
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Execute a backup
    /// </summary>
    Task<BackupExecution> ExecuteBackupAsync(string backupId, CancellationToken ct = default);

    /// <summary>
    /// Get backup execution history
    /// </summary>
    Task<List<BackupExecution>> GetBackupHistoryAsync(string backupId, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Validate backup integrity
    /// </summary>
    Task<bool> ValidateBackupAsync(string backupExecutionId, CancellationToken ct = default);

    /// <summary>
    /// Run a restore test
    /// </summary>
    Task<RestoreTest> RunRestoreTestAsync(string backupId, CancellationToken ct = default);

    /// <summary>
    /// Get restore test history
    /// </summary>
    Task<List<RestoreTest>> GetRestoreTestHistoryAsync(string backupId, CancellationToken ct = default);
}


