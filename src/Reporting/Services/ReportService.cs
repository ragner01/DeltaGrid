using IOC.Reporting.Models;
using IOC.Reporting.Templates;
using IOC.Reporting.Export;
using System.Security.Cryptography;
using System.Text;

namespace IOC.Reporting.Services;

/// <summary>
/// Report generation service implementation
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IPdfExporter _pdfExporter;
    private readonly IExcelExporter _excelExporter;
    private readonly ICsvExporter _csvExporter;
    private readonly IReportRepository _repository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ITemplateEngine templateEngine,
        IPdfExporter pdfExporter,
        IExcelExporter excelExporter,
        ICsvExporter csvExporter,
        IReportRepository repository,
        ILogger<ReportService> logger)
    {
        _templateEngine = templateEngine;
        _pdfExporter = pdfExporter;
        _excelExporter = excelExporter;
        _csvExporter = csvExporter;
        _repository = repository;
        _logger = logger;
    }

    public async Task<GeneratedReport> GenerateAsync(ReportRequest request, CancellationToken ct = default)
    {
        var template = await _repository.GetTemplateAsync(request.TemplateId, ct);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {request.TemplateId} not found");
        }

        // Render template
        var rendered = await _templateEngine.RenderAsync(template, request.Parameters, ct);

        // Export to requested format
        byte[] content;
        string contentType;
        string fileName;

        switch (request.Format)
        {
            case ReportFormat.PDF:
                var watermark = request.RequireSignature ? "DRAFT" : null;
                content = await _pdfExporter.ExportAsync(rendered, watermark, ct);
                contentType = "application/pdf";
                fileName = $"report_{request.TemplateId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                break;

            case ReportFormat.Excel:
                content = await _excelExporter.ExportAsync(rendered, ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"report_{request.TemplateId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                break;

            case ReportFormat.CSV:
                content = await _csvExporter.ExportAsync(rendered, ct);
                contentType = "text/csv";
                fileName = $"report_{request.TemplateId}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
                break;

            default:
                throw new NotSupportedException($"Format {request.Format} not supported");
        }

        var report = new GeneratedReport
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = template.Id,
            TemplateVersion = template.Version,
            Type = template.Type,
            TenantId = request.TenantId,
            Content = content,
            Format = request.Format,
            ContentType = contentType,
            FileName = fileName,
            GeneratedAt = DateTimeOffset.UtcNow,
            ReportDate = request.ReportDate ?? DateTimeOffset.UtcNow.Date,
            SiteId = request.SiteId,
            AssetId = request.AssetId,
            Parameters = request.Parameters,
            Status = request.RequireSignature ? ReportStatus.PendingApproval : ReportStatus.Published,
            Watermark = request.RequireSignature ? "DRAFT" : null
        };

        await _repository.SaveReportAsync(report, ct);

        _logger.LogInformation("Generated report {ReportId} from template {TemplateId}", report.Id, template.Id);

        return report;
    }

    public async Task<ReportSignature> SignAsync(string reportId, string signerId, string signerName, string role, string? comment = null, CancellationToken ct = default)
    {
        var report = await _repository.GetReportAsync(reportId, ct);
        if (report == null)
        {
            throw new InvalidOperationException($"Report {reportId} not found");
        }

        // Compute signature hash (HMAC-SHA256 of report content + signer)
        var signerData = $"{reportId}|{signerId}|{report.TemplateId}|{report.TemplateVersion}";
        using var hmac = new HMACSHA256(report.Content);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signerData));
        var signatureHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        var signature = new ReportSignature
        {
            ReportId = reportId,
            SignerId = signerId,
            SignerName = signerName,
            Role = role,
            SignedAt = DateTimeOffset.UtcNow,
            SignatureHash = signatureHash,
            Comment = comment
        };

        await _repository.SaveSignatureAsync(signature, ct);

        // Update report status if all required signatures present
        var existingSignatures = await _repository.GetSignaturesAsync(reportId, ct);
        // TODO: Check if all required signatures are present (based on template/region requirements)

        _logger.LogInformation("Report {ReportId} signed by {SignerName} ({Role})", reportId, signerName, role);

        return signature;
    }

    public async Task ArchiveAsync(string reportId, CancellationToken ct = default)
    {
        var report = await _repository.GetReportAsync(reportId, ct);
        if (report == null)
        {
            throw new InvalidOperationException($"Report {reportId} not found");
        }

        var archive = new ReportArchive
        {
            ReportId = reportId,
            Content = report.Content,
            ContentType = report.ContentType,
            FileName = report.FileName,
            ArchivedAt = DateTimeOffset.UtcNow,
            TenantId = report.TenantId,
            AccessLogs = new List<ReportAccessLog>()
        };

        await _repository.ArchiveReportAsync(archive, ct);

        _logger.LogInformation("Report {ReportId} archived", reportId);
    }

    public Task<ReportArchive?> GetArchiveAsync(string reportId, CancellationToken ct = default)
    {
        return _repository.GetArchiveAsync(reportId, ct);
    }

    public async Task LogAccessAsync(string reportId, string userId, string userName, string action, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        var logEntry = new ReportAccessLog
        {
            UserId = userId,
            UserName = userName,
            AccessedAt = DateTimeOffset.UtcNow,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _repository.LogAccessAsync(reportId, logEntry, ct);

        _logger.LogInformation("Report {ReportId} accessed by {UserName}: {Action}", reportId, userName, action);
    }
}

/// <summary>
/// Report repository interface
/// </summary>
public interface IReportRepository
{
    Task<ReportTemplate?> GetTemplateAsync(string templateId, CancellationToken ct = default);
    Task SaveReportAsync(GeneratedReport report, CancellationToken ct = default);
    Task<GeneratedReport?> GetReportAsync(string reportId, CancellationToken ct = default);
    Task SaveSignatureAsync(ReportSignature signature, CancellationToken ct = default);
    Task<List<ReportSignature>> GetSignaturesAsync(string reportId, CancellationToken ct = default);
    Task ArchiveReportAsync(ReportArchive archive, CancellationToken ct = default);
    Task<ReportArchive?> GetArchiveAsync(string reportId, CancellationToken ct = default);
    Task LogAccessAsync(string reportId, ReportAccessLog logEntry, CancellationToken ct = default);
    Task SaveScheduleAsync(ScheduledReport schedule, CancellationToken ct = default);
    Task<ScheduledReport?> GetScheduleAsync(string scheduleId, CancellationToken ct = default);
    Task DeleteScheduleAsync(string scheduleId, CancellationToken ct = default);
    Task SetScheduleEnabledAsync(string scheduleId, bool enabled, CancellationToken ct = default);
    Task UpdateScheduleLastRunAsync(string scheduleId, DateTimeOffset lastRun, CancellationToken ct = default);
}

