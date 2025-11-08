using Hangfire;
using Hangfire.Storage;
using IOC.Reporting.Models;
using IOC.Reporting.Services;

namespace IOC.Reporting.Scheduling;

/// <summary>
/// Hangfire-based report scheduler
/// </summary>
public interface IReportScheduler
{
    /// <summary>
    /// Schedule a report for recurring generation
    /// </summary>
    Task<string> ScheduleAsync(ScheduledReport schedule, CancellationToken ct = default);

    /// <summary>
    /// Update an existing schedule
    /// </summary>
    Task UpdateScheduleAsync(string scheduleId, ScheduledReport schedule, CancellationToken ct = default);

    /// <summary>
    /// Delete a schedule
    /// </summary>
    Task DeleteScheduleAsync(string scheduleId, CancellationToken ct = default);

    /// <summary>
    /// Enable/disable a schedule
    /// </summary>
    Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Trigger immediate execution of a scheduled report
    /// </summary>
    Task TriggerNowAsync(string scheduleId, CancellationToken ct = default);
}

/// <summary>
/// Hangfire implementation of report scheduler
/// </summary>
public sealed class HangfireReportScheduler : IReportScheduler
{
    private readonly IReportService _reportService;
    private readonly IReportRepository _repository;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<HangfireReportScheduler> _logger;
    private static readonly RecurringJobOptions UtcRecurringOptions = new() { TimeZone = TimeZoneInfo.Utc };

    public HangfireReportScheduler(
        IReportService reportService,
        IReportRepository repository,
        IBackgroundJobClient jobClient,
        ILogger<HangfireReportScheduler> logger)
    {
        _reportService = reportService;
        _repository = repository;
        _jobClient = jobClient;
        _logger = logger;
    }

    public async Task<string> ScheduleAsync(ScheduledReport schedule, CancellationToken ct = default)
    {
        RecurringJob.AddOrUpdate(
            schedule.Id,
            () => ExecuteScheduledReportAsync(schedule.Id, ct),
            schedule.CronExpression,
            UtcRecurringOptions);

        await _repository.SaveScheduleAsync(schedule, ct);

        _logger.LogInformation("Scheduled report {ScheduleId} with cron {Cron}", schedule.Id, schedule.CronExpression);

        return schedule.Id;
    }

    public Task UpdateScheduleAsync(string scheduleId, ScheduledReport schedule, CancellationToken ct = default)
    {
        RecurringJob.AddOrUpdate(
            scheduleId,
            () => ExecuteScheduledReportAsync(scheduleId, ct),
            schedule.CronExpression,
            UtcRecurringOptions);

        return _repository.SaveScheduleAsync(schedule, ct);
    }

    public Task DeleteScheduleAsync(string scheduleId, CancellationToken ct = default)
    {
        RecurringJob.RemoveIfExists(scheduleId);
        return _repository.DeleteScheduleAsync(scheduleId, ct);
    }

    public async Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken ct = default)
    {
        if (enabled)
        {
            var schedule = await _repository.GetScheduleAsync(scheduleId, ct);
            if (schedule == null)
            {
                throw new InvalidOperationException($"Schedule {scheduleId} not found");
            }

            RecurringJob.AddOrUpdate(scheduleId, () => ExecuteScheduledReportAsync(scheduleId, ct), schedule.CronExpression, UtcRecurringOptions);
        }
        else
        {
            RecurringJob.RemoveIfExists(scheduleId);
        }

        await _repository.SetScheduleEnabledAsync(scheduleId, enabled, ct);
    }

    public Task TriggerNowAsync(string scheduleId, CancellationToken ct = default)
    {
        _jobClient.Enqueue(() => ExecuteScheduledReportAsync(scheduleId, ct));
        return Task.CompletedTask;
    }

    public async Task ExecuteScheduledReportAsync(string scheduleId, CancellationToken ct = default)
    {
        try
        {
            var schedule = await _repository.GetScheduleAsync(scheduleId, ct);
            if (schedule == null || !schedule.IsEnabled)
            {
                _logger.LogWarning("Schedule {ScheduleId} not found or disabled", scheduleId);
                return;
            }

            var request = new ReportRequest
            {
                TemplateId = schedule.TemplateId,
                TenantId = schedule.TenantId,
                Parameters = schedule.Parameters,
                Format = schedule.Format,
                DistributionList = schedule.DistributionList,
                RequireSignature = false
            };

            var report = await _reportService.GenerateAsync(request, ct);

            // TODO: Send email to distribution list
            _logger.LogInformation("Executed scheduled report {ScheduleId}, generated {ReportId}", scheduleId, report.Id);

            await _repository.UpdateScheduleLastRunAsync(scheduleId, DateTimeOffset.UtcNow, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scheduled report {ScheduleId}", scheduleId);
            throw;
        }
    }
}
