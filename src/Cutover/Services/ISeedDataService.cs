using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Seed data service for production cutover
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seed all demo data
    /// </summary>
    Task<SeedResult> SeedAllAsync(string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Seed specific data type
    /// </summary>
    Task<SeedResult> SeedAsync(SeedDataType type, string createdBy, CancellationToken ct = default);

    /// <summary>
    /// Validate seed data
    /// </summary>
    Task<ValidationResult> ValidateAsync(CancellationToken ct = default);

    /// <summary>
    /// Clear seed data (dry-run cleanup)
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);
}

/// <summary>
/// Seed result
/// </summary>
public sealed class SeedResult
{
    public required SeedDataType Type { get; init; }
    public int RecordsCreated { get; init; }
    public int RecordsFailed { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Validation result
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public List<ValidationIssue> Issues { get; init; } = new();
    public Dictionary<SeedDataType, int> RecordCounts { get; init; } = new();
}

/// <summary>
/// Validation issue
/// </summary>
public sealed class ValidationIssue
{
    public required SeedDataType Type { get; init; }
    public required string Issue { get; init; }
    public required string Severity { get; init; }  // Error, Warning
}

