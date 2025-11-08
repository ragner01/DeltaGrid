using IOC.Reporting.Models;

namespace IOC.Reporting.Services;

/// <summary>
/// Report generation service
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generate a report from a template and parameters
    /// </summary>
    Task<GeneratedReport> GenerateAsync(ReportRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sign a report for approval workflow
    /// </summary>
    Task<ReportSignature> SignAsync(string reportId, string signerId, string signerName, string role, string? comment = null, CancellationToken ct = default);

    /// <summary>
    /// Archive a report with access logs
    /// </summary>
    Task ArchiveAsync(string reportId, CancellationToken ct = default);

    /// <summary>
    /// Get report archive with access log
    /// </summary>
    Task<ReportArchive?> GetArchiveAsync(string reportId, CancellationToken ct = default);

    /// <summary>
    /// Record access log entry
    /// </summary>
    Task LogAccessAsync(string reportId, string userId, string userName, string action, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default);
}


