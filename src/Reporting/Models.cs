namespace IOC.Reporting.Models;

/// <summary>
/// Report types for different operational and regulatory needs
/// </summary>
public enum ReportType
{
    DailyProduction,
    DrillingOperations,
    HSEKpis,
    Flaring,
    Losses,
    Deferments,
    OPEX,
    CustodyTransfer,
    Allocation,
    Integrity,
    LabResults,
    Compliance
}

/// <summary>
/// Report template definition with versioning
/// </summary>
public sealed class ReportTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ReportType Type { get; init; }
    public required string Version { get; init; }
    public required string TemplateContent { get; init; } // JSON/YAML template definition
    public required List<string> Parameters { get; init; } // Parameter names expected
    public string? Region { get; init; } // Compliance variant by region
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; init; }
    public bool IsActive { get; init; } = true;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Rendered report artifact ready for export
/// </summary>
public sealed class RenderedReport
{
    public required string TemplateId { get; init; }
    public required ReportType Type { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public object? Data { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Report generation request with parameters
/// </summary>
public sealed class ReportRequest
{
    public required string TemplateId { get; init; }
    public required string TenantId { get; init; }
    public required Dictionary<string, object> Parameters { get; init; }
    public DateTimeOffset? ReportDate { get; init; }
    public string? SiteId { get; init; }
    public string? AssetId { get; init; }
    public ReportFormat Format { get; init; } = ReportFormat.PDF;
    public bool RequireSignature { get; init; } = false;
    public List<string>? DistributionList { get; init; } // Email addresses
}

/// <summary>
/// Report output formats
/// </summary>
public enum ReportFormat
{
    PDF,
    Excel,
    CSV,
    HTML
}

/// <summary>
/// Generated report with metadata
/// </summary>
public sealed class GeneratedReport
{
    public required string Id { get; init; }
    public required string TemplateId { get; init; }
    public required string TemplateVersion { get; init; }
    public required ReportType Type { get; init; }
    public required string TenantId { get; init; }
    public required byte[] Content { get; init; }
    public required ReportFormat Format { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? GeneratedBy { get; init; }
    public DateTimeOffset? ReportDate { get; init; }
    public string? SiteId { get; init; }
    public string? AssetId { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public ReportStatus Status { get; init; } = ReportStatus.Draft;
    public string? Watermark { get; init; } // Watermark text (e.g., "DRAFT", "CONFIDENTIAL")
    public string? SignatureHash { get; init; } // Hash of signatures for verification
}

/// <summary>
/// Report status in workflow
/// </summary>
public enum ReportStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Published,
    Archived
}

/// <summary>
/// Report signature for approval workflow
/// </summary>
public sealed class ReportSignature
{
    public required string ReportId { get; init; }
    public required string SignerId { get; init; }
    public required string SignerName { get; init; }
    public required string Role { get; init; }
    public required DateTimeOffset SignedAt { get; init; }
    public required string SignatureHash { get; init; } // HMAC-SHA256 of report content + signer
    public string? Comment { get; init; }
}

/// <summary>
/// Scheduled report job definition
/// </summary>
public sealed record ScheduledReport
{
    public required string Id { get; init; }
    public required string TemplateId { get; init; }
    public required string TenantId { get; init; }
    public required string CronExpression { get; init; } // Quartz cron
    public required Dictionary<string, object> Parameters { get; init; }
    public required List<string> DistributionList { get; init; }
    public required ReportFormat Format { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTimeOffset? LastRunAt { get; init; }
    public DateTimeOffset? NextRunAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Report archive record with access logs
/// </summary>
public sealed class ReportArchive
{
    public required string ReportId { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public required DateTimeOffset ArchivedAt { get; init; }
    public required string TenantId { get; init; }
    public List<ReportAccessLog> AccessLogs { get; init; } = new();
}

/// <summary>
/// Access log entry for audit trail
/// </summary>
public sealed class ReportAccessLog
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required DateTimeOffset AccessedAt { get; init; }
    public required string Action { get; init; } // "VIEWED", "DOWNLOADED", "EXPORTED"
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// Report catalogue entry
/// </summary>
public sealed class ReportCatalogue
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ReportType Type { get; init; }
    public required string Owner { get; init; } // Owner role or user
    public string? Description { get; init; }
    public required List<string> AvailableFormats { get; init; }
    public string? Region { get; init; }
    public List<string> RequiredRoles { get; init; } = new();
    public DateTimeOffset? LastGeneratedAt { get; init; }
    public int GenerationCount { get; init; } = 0;
}


