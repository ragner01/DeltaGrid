namespace IOC.DataGovernance.Models;

/// <summary>
/// Data quality dimensions
/// </summary>
public enum DqDimension
{
    Completeness,  // % of non-null values
    Timeliness,    // Data freshness
    Validity,       // Data format/conformance
    Consistency    // Cross-field/cross-dataset consistency
}

/// <summary>
/// Data quality rule definition
/// </summary>
public sealed record DqRule
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DatasetId { get; init; }  // Dataset identifier (table, view, etc.)
    public required DqDimension Dimension { get; init; }
    public required string Expression { get; init; }  // SQL expression, regex, etc.
    public double Threshold { get; init; }  // Threshold value (0-100 for completeness, minutes for timeliness, etc.)
    public DqThresholdOperator Operator { get; init; }  // GreaterThan, LessThan, Equal, NotEqual
    public string? Description { get; init; }
    public string? Owner { get; init; }  // Steward/owner
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastEvaluatedAt { get; init; }
}

/// <summary>
/// Threshold operators
/// </summary>
public enum DqThresholdOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual
}

/// <summary>
/// Data quality score
/// </summary>
public sealed record DqScore
{
    public required string RuleId { get; init; }
    public required string DatasetId { get; init; }
    public required DqDimension Dimension { get; init; }
    public required DateTimeOffset EvaluatedAt { get; init; }
    public double Score { get; init; }  // Actual score value
    public double Threshold { get; init; }
    public bool Passed { get; init; }  // Whether threshold met
    public string? Details { get; init; }  // Additional details (sample records, etc.)
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Data quality breach
/// </summary>
public sealed record DqBreach
{
    public required string Id { get; init; }
    public required string RuleId { get; init; }
    public required string DatasetId { get; init; }
    public required DqDimension Dimension { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public DqBreachStatus Status { get; init; } = DqBreachStatus.Open;
    public double ActualScore { get; init; }
    public double Threshold { get; init; }
    public string? ExceptionId { get; init; }  // Exception workflow ID
    public string? RemediationNotes { get; init; }
    public string? ResolvedBy { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Breach status
/// </summary>
public enum DqBreachStatus
{
    Open,
    Acknowledged,
    InProgress,
    Resolved,
    Exception
}

/// <summary>
/// Data quality exception
/// </summary>
public sealed record DqException
{
    public required string Id { get; init; }
    public required string BreachId { get; init; }
    public required string RequestedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }  // Auto-expiry for time-bound exceptions
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public DqExceptionStatus Status { get; init; } = DqExceptionStatus.Pending;
    public string? RejectionReason { get; init; }
}

/// <summary>
/// Exception status
/// </summary>
public enum DqExceptionStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}

/// <summary>
/// Data access request
/// </summary>
public sealed record AccessRequest
{
    public required string Id { get; init; }
    public required string RequestedBy { get; init; }
    public required string DatasetId { get; init; }
    public required AccessLevel Level { get; init; }
    public required string Justification { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }  // Time-bound access
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public AccessRequestStatus Status { get; init; } = AccessRequestStatus.Pending;
    public string? RejectionReason { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Access levels
/// </summary>
public enum AccessLevel
{
    Read,       // Read-only access
    Write,      // Write access
    Delete,     // Delete access
    Admin       // Administrative access
}

/// <summary>
/// Access request status
/// </summary>
public enum AccessRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Revoked
}

/// <summary>
/// Data lineage record
/// </summary>
public sealed record DataLineage
{
    public required string Id { get; init; }
    public required string SourceId { get; init; }  // Source dataset
    public required string TargetId { get; init; }  // Target dataset
    public required LineageType Type { get; init; }
    public required string Transformation { get; init; }  // Description of transformation
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Lineage types
/// </summary>
public enum LineageType
{
    Direct,      // Direct copy
    Transform,   // Transformation
    Aggregate,   // Aggregation
    Join         // Join/merge
}

/// <summary>
/// Impact assessment result
/// </summary>
public sealed record ImpactAssessment
{
    public required string Id { get; init; }
    public required string SourceBreachId { get; init; }
    public required List<string> AffectedDatasets { get; init; }
    public required List<string> AffectedReports { get; init; }
    public required List<string> AffectedServices { get; init; }
    public required DateTimeOffset AssessedAt { get; init; }
    public ImpactSeverity Severity { get; init; }
    public string? Recommendations { get; init; }
}

/// <summary>
/// Impact severity
/// </summary>
public enum ImpactSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Dataset metadata
/// </summary>
public sealed record DatasetMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Owner { get; init; }  // Data steward
    public required DatasetClassification Classification { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUpdatedAt { get; init; }
}

/// <summary>
/// Dataset classification
/// </summary>
public enum DatasetClassification
{
    Public,
    Internal,
    Confidential,
    Restricted
}

