namespace IOC.DisasterRecovery.Models;

/// <summary>
/// Disaster Recovery service tiers
/// </summary>
public enum DrTier
{
    Critical,    // RTO: < 1 hour, RPO: < 15 minutes
    High,         // RTO: < 4 hours, RPO: < 1 hour
    Standard,     // RTO: < 24 hours, RPO: < 4 hours
    Low          // RTO: < 72 hours, RPO: < 24 hours
}

/// <summary>
/// Service DR classification
/// </summary>
public sealed record ServiceDrClassification
{
    public required string ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public required DrTier Tier { get; init; }
    public TimeSpan Rto { get; init; }  // Recovery Time Objective
    public TimeSpan Rpo { get; init; }  // Recovery Point Objective
    public bool GeoRedundant { get; init; }
    public bool AutomatedFailover { get; init; }
    public List<string> Dependencies { get; init; } = new();  // Dependent services
    public string? BackupStrategy { get; init; }
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Backup definition
/// </summary>
public sealed record BackupDefinition
{
    public required string Id { get; init; }
    public required string ServiceId { get; init; }
    public required BackupType Type { get; init; }
    public required string Source { get; init; }  // Database name, storage account, etc.
    public required string Destination { get; init; }  // Backup storage location
    public required string Schedule { get; init; }  // Cron expression
    public int RetentionDays { get; init; } = 30;
    public bool Encrypted { get; init; } = true;
    public DateTimeOffset LastBackupAt { get; init; }
    public DateTimeOffset? NextBackupAt { get; init; }
    public BackupStatus Status { get; init; } = BackupStatus.Scheduled;
}

/// <summary>
/// Backup types
/// </summary>
public enum BackupType
{
    Full,
    Differential,
    Incremental,
    TransactionLog,
    Snapshot
}

/// <summary>
/// Backup status
/// </summary>
public enum BackupStatus
{
    Scheduled,
    InProgress,
    Completed,
    Failed,
    Expired
}

/// <summary>
/// Backup execution record
/// </summary>
public sealed record BackupExecution
{
    public required string Id { get; init; }
    public required string BackupId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public BackupStatus Status { get; init; }
    public long? SizeBytes { get; init; }
    public string? Location { get; init; }  // Backup storage location
    public string? Checksum { get; init; }  // SHA-256 checksum
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Restore test definition
/// </summary>
public sealed record RestoreTest
{
    public required string Id { get; init; }
    public required string BackupId { get; init; }
    public required DateTimeOffset ScheduledAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public RestoreTestStatus Status { get; init; } = RestoreTestStatus.Scheduled;
    public bool IntegrityVerified { get; init; } = false;
    public string? IntegrityChecksum { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> ValidationResults { get; init; } = new();
}

/// <summary>
/// Restore test status
/// </summary>
public enum RestoreTestStatus
{
    Scheduled,
    InProgress,
    Completed,
    Failed,
    IntegrityFailed
}

/// <summary>
/// Failover configuration
/// </summary>
public sealed record FailoverConfiguration
{
    public required string Id { get; init; }
    public required string ServiceId { get; init; }
    public required string PrimaryRegion { get; init; }
    public required string SecondaryRegion { get; init; }
    public FailoverMode Mode { get; init; }  // Automatic, Manual
    public bool IsActive { get; init; } = true;
    public DateTimeOffset? LastFailoverAt { get; init; }
    public DateTimeOffset? LastFailoverTestAt { get; init; }
}

/// <summary>
/// Failover modes
/// </summary>
public enum FailoverMode
{
    Automatic,
    Manual,
    Test
}

/// <summary>
/// DR drill execution
/// </summary>
public sealed record DrDrill
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DrDrillType Type { get; init; }
    public required DateTimeOffset ScheduledAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DrDrillStatus Status { get; init; } = DrDrillStatus.Scheduled;
    public List<string> Services { get; init; } = new();  // Services involved in drill
    public Dictionary<string, DrDrillResult> Results { get; init; } = new();  // Service ID -> Result
    public TimeSpan? ActualRto { get; init; }
    public TimeSpan? ActualRpo { get; init; }
    public bool MetRto { get; init; }
    public bool MetRpo { get; init; }
    public string? PostmortemId { get; init; }
}

/// <summary>
/// DR drill types
/// </summary>
public enum DrDrillType
{
    FullSiteFailure,
    DatabaseFailure,
    StorageFailure,
    NetworkFailure,
    ServiceFailure
}

/// <summary>
/// DR drill status
/// </summary>
public enum DrDrillStatus
{
    Scheduled,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// DR drill result per service
/// </summary>
public sealed record DrDrillResult
{
    public required string ServiceId { get; init; }
    public TimeSpan? RecoveryTime { get; init; }
    public TimeSpan? DataLossWindow { get; init; }
    public bool MetRto { get; init; }
    public bool MetRpo { get; init; }
    public bool DataIntegrityVerified { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// DR readiness status
/// </summary>
public sealed record DrReadinessStatus
{
    public required string ServiceId { get; init; }
    public required DrTier Tier { get; init; }
    public DrReadinessLevel Level { get; init; }
    public DateTimeOffset LastBackupAt { get; init; }
    public DateTimeOffset? LastRestoreTestAt { get; init; }
    public DateTimeOffset? LastDrillAt { get; init; }
    public bool GeoRedundantConfigured { get; init; }
    public bool FailoverConfigured { get; init; }
    public bool BackupValidationPassed { get; init; }
    public List<string> Issues { get; init; } = new();
}

/// <summary>
/// DR readiness levels
/// </summary>
public enum DrReadinessLevel
{
    Ready,        // All requirements met
    Warning,      // Minor issues
    Critical,     // Critical issues preventing recovery
    Unknown       // Status not determined
}


