namespace IOC.Cutover.Models;

/// <summary>
/// Seed data types
/// </summary>
public enum SeedDataType
{
    Tenant,
    Asset,
    Well,
    Meter,
    LabReference,
    User,
    Role
}

/// <summary>
/// Seed data definition
/// </summary>
public sealed record SeedData
{
    public required string Id { get; init; }
    public required SeedDataType Type { get; init; }
    public required string Name { get; init; }
    public required object Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; init; }
}

/// <summary>
/// Cutover phase
/// </summary>
public enum CutoverPhase
{
    Planning,
    Preparation,
    DryRun,
    Execution,
    Stabilization,
    Hypercare,
    Complete,
    Rollback
}

/// <summary>
/// Cutover checklist item
/// </summary>
public sealed record CutoverChecklistItem
{
    public required string Id { get; init; }
    public required string Phase { get; init; }
    public required string Task { get; init; }
    public string? Description { get; init; }
    public string? Owner { get; init; }
    public bool IsCompleted { get; init; } = false;
    public DateTimeOffset? CompletedAt { get; init; }
    public string? CompletedBy { get; init; }
    public bool IsCritical { get; init; } = false;
    public string? Notes { get; init; }
}

/// <summary>
/// Cutover execution
/// </summary>
public sealed record CutoverExecution
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CutoverPhase Phase { get; init; }
    public required DateTimeOffset PlannedStart { get; init; }
    public DateTimeOffset? ActualStart { get; init; }
    public DateTimeOffset? PlannedEnd { get; init; }
    public DateTimeOffset? ActualEnd { get; init; }
    public CutoverStatus Status { get; init; } = CutoverStatus.Pending;
    public List<CutoverChecklistItem> Checklist { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public string? RollbackPlanId { get; init; }
}

/// <summary>
/// Cutover status
/// </summary>
public enum CutoverStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    RolledBack,
    Cancelled
}

/// <summary>
/// Readiness criteria
/// </summary>
public sealed record ReadinessCriteria
{
    public required string Module { get; init; }
    public required string Criterion { get; init; }
    public string? Description { get; init; }
    public bool IsMet { get; init; } = false;
    public DateTimeOffset? MetAt { get; init; }
    public string? ValidatedBy { get; init; }
    public bool IsCritical { get; init; } = false;
}

/// <summary>
/// Feature flag
/// </summary>
public sealed record FeatureFlag
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Module { get; init; }
    public required bool IsEnabled { get; init; } = false;
    public FeatureFlagStrategy Strategy { get; init; } = FeatureFlagStrategy.Progressive;
    public double RolloutPercentage { get; init; } = 0.0;  // 0-100
    public List<string> EnabledTenants { get; init; } = new();
    public List<string> EnabledUsers { get; init; } = new();
    public DateTimeOffset? EnabledAt { get; init; }
    public DateTimeOffset? ScheduledEnableAt { get; init; }
    public string? Description { get; init; }
    public bool IsRisky { get; init; } = false;
}

/// <summary>
/// Feature flag strategy
/// </summary>
public enum FeatureFlagStrategy
{
    AllOrNothing,     // Enable for all or none
    Progressive,      // Gradual rollout
    TenantBased,      // Enable per tenant
    UserBased         // Enable per user
}

/// <summary>
/// Rollback plan
/// </summary>
public sealed record RollbackPlan
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string CutoverId { get; init; }
    public required List<RollbackStep> Steps { get; init; } = new();
    public RollbackStatus Status { get; init; } = RollbackStatus.Ready;
    public DateTimeOffset? ExecutedAt { get; init; }
    public string? ExecutedBy { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Rollback step
/// </summary>
public sealed record RollbackStep
{
    public required int Order { get; init; }
    public required string Action { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; init; } = false;
    public DateTimeOffset? CompletedAt { get; init; }
    public string? CompletedBy { get; init; }
    public bool IsCritical { get; init; } = false;
}

/// <summary>
/// Rollback status
/// </summary>
public enum RollbackStatus
{
    Ready,
    InProgress,
    Completed,
    Failed,
    NotRequired
}

/// <summary>
/// Hypercare incident
/// </summary>
public sealed record HypercareIncident
{
    public required string Id { get; init; }
    public required string CutoverId { get; init; }
    public required IncidentSeverity Severity { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Module { get; init; }
    public required DateTimeOffset ReportedAt { get; init; }
    public string? ReportedBy { get; init; }
    public HypercareIncidentStatus Status { get; init; } = HypercareIncidentStatus.Open;
    public string? AssignedTo { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? Resolution { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Incident severity
/// </summary>
public enum IncidentSeverity
{
    Sev1,  // Critical - System down
    Sev2,  // High - Major feature broken
    Sev3,  // Medium - Minor feature broken
    Sev4   // Low - Cosmetic issue
}

/// <summary>
/// Hypercare incident status
/// </summary>
public enum HypercareIncidentStatus
{
    Open,
    Assigned,
    InProgress,
    Resolved,
    Closed
}

/// <summary>
/// Training material
/// </summary>
public sealed record TrainingMaterial
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required TrainingMaterialType Type { get; init; }
    public required string Module { get; init; }
    public string? Description { get; init; }
    public required string Content { get; init; }  // Markdown, PDF path, etc.
    public List<string> Tags { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; init; }
}

/// <summary>
/// Training material type
/// </summary>
public enum TrainingMaterialType
{
    Playbook,      // Step-by-step playbook
    Video,         // Video tutorial
    Documentation, // Documentation
    Scenario       // Scenario-based training
}


