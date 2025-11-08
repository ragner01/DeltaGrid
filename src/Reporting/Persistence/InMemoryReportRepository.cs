using IOC.Reporting.Models;
using IOC.Reporting.Services;

namespace IOC.Reporting.Persistence;

/// <summary>
/// In-memory implementation of report repository
/// </summary>
public sealed class InMemoryReportRepository : IReportRepository
{
    private readonly Dictionary<string, ReportTemplate> _templates = new();
    private readonly Dictionary<string, GeneratedReport> _reports = new();
    private readonly Dictionary<string, List<ReportSignature>> _signatures = new();
    private readonly Dictionary<string, ReportArchive> _archives = new();
    private readonly Dictionary<string, ScheduledReport> _schedules = new();

    public Task<ReportTemplate?> GetTemplateAsync(string templateId, CancellationToken ct = default)
    {
        return Task.FromResult(_templates.TryGetValue(templateId, out var template) ? template : null);
    }

    public Task SaveReportAsync(GeneratedReport report, CancellationToken ct = default)
    {
        _reports[report.Id] = report;
        return Task.CompletedTask;
    }

    public Task<GeneratedReport?> GetReportAsync(string reportId, CancellationToken ct = default)
    {
        return Task.FromResult(_reports.TryGetValue(reportId, out var report) ? report : null);
    }

    public Task SaveSignatureAsync(ReportSignature signature, CancellationToken ct = default)
    {
        if (!_signatures.TryGetValue(signature.ReportId, out var sigs))
        {
            sigs = new List<ReportSignature>();
            _signatures[signature.ReportId] = sigs;
        }
        sigs.Add(signature);
        return Task.CompletedTask;
    }

    public Task<List<ReportSignature>> GetSignaturesAsync(string reportId, CancellationToken ct = default)
    {
        return Task.FromResult(_signatures.TryGetValue(reportId, out var sigs) ? sigs : new List<ReportSignature>());
    }

    public Task ArchiveReportAsync(ReportArchive archive, CancellationToken ct = default)
    {
        _archives[archive.ReportId] = archive;
        return Task.CompletedTask;
    }

    public Task<ReportArchive?> GetArchiveAsync(string reportId, CancellationToken ct = default)
    {
        return Task.FromResult(_archives.TryGetValue(reportId, out var archive) ? archive : null);
    }

    public Task LogAccessAsync(string reportId, ReportAccessLog logEntry, CancellationToken ct = default)
    {
        if (_archives.TryGetValue(reportId, out var archive))
        {
            archive.AccessLogs.Add(logEntry);
        }
        return Task.CompletedTask;
    }

    // Scheduling extensions
    public Task SaveScheduleAsync(ScheduledReport schedule, CancellationToken ct = default)
    {
        _schedules[schedule.Id] = schedule;
        return Task.CompletedTask;
    }

    public Task<ScheduledReport?> GetScheduleAsync(string scheduleId, CancellationToken ct = default)
    {
        return Task.FromResult(_schedules.TryGetValue(scheduleId, out var schedule) ? schedule : null);
    }

    public Task DeleteScheduleAsync(string scheduleId, CancellationToken ct = default)
    {
        _schedules.Remove(scheduleId);
        return Task.CompletedTask;
    }

    public Task SetScheduleEnabledAsync(string scheduleId, bool enabled, CancellationToken ct = default)
    {
        if (_schedules.TryGetValue(scheduleId, out var existingSchedule))
        {
            _schedules[scheduleId] = existingSchedule with { IsEnabled = enabled };
        }
        return Task.CompletedTask;
    }

    public Task UpdateScheduleLastRunAsync(string scheduleId, DateTimeOffset lastRun, CancellationToken ct = default)
    {
        if (_schedules.TryGetValue(scheduleId, out var existingSchedule))
        {
            _schedules[scheduleId] = existingSchedule with { LastRunAt = lastRun };
        }
        return Task.CompletedTask;
    }

    // Seed some templates
    public void SeedTemplates()
    {
        _templates["daily-prod-1"] = new ReportTemplate
        {
            Id = "daily-prod-1",
            Name = "Daily Production Report",
            Type = ReportType.DailyProduction,
            Version = "1.0",
            TemplateContent = """{"title": "Daily Production Report", "sections": [{"title": "Summary", "content": "{{date}}"}]}""",
            Parameters = new List<string> { "date", "siteId" },
            Region = "Nigeria",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

