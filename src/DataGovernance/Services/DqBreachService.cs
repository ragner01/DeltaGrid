using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data quality breach service implementation
/// </summary>
public sealed class DqBreachService : IDqBreachService
{
    private readonly IDqBreachRepository _breachRepository;
    private readonly IDqEngine _dqEngine;
    private readonly ILogger<DqBreachService> _logger;

    public DqBreachService(
        IDqBreachRepository breachRepository,
        IDqEngine dqEngine,
        ILogger<DqBreachService> logger)
    {
        _breachRepository = breachRepository;
        _dqEngine = dqEngine;
        _logger = logger;
    }

    public async Task<List<DqBreach>> DetectBreachesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Detecting data quality breaches");

        var scores = await _dqEngine.EvaluateAllRulesAsync(ct);
        var breaches = new List<DqBreach>();

        foreach (var score in scores.Where(s => !s.Passed))
        {
            // Check if breach already exists
            var existingBreach = await _breachRepository.GetOpenBreachByRuleAsync(score.RuleId, ct);
            
            if (existingBreach == null)
            {
                // Create new breach
                var breach = new DqBreach
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleId = score.RuleId,
                    DatasetId = score.DatasetId,
                    Dimension = score.Dimension,
                    DetectedAt = DateTimeOffset.UtcNow,
                    Status = DqBreachStatus.Open,
                    ActualScore = score.Score,
                    Threshold = score.Threshold
                };

                await _breachRepository.SaveBreachAsync(breach, ct);
                breaches.Add(breach);

                _logger.LogWarning("DQ breach detected: Rule {RuleId}, Dataset {DatasetId}, Score {Score}, Threshold {Threshold}",
                    score.RuleId, score.DatasetId, score.Score, score.Threshold);
            }
            else
            {
                // Update existing breach
                existingBreach = existingBreach with
                {
                    ActualScore = score.Score,
                    Status = existingBreach.Status == DqBreachStatus.Resolved ? DqBreachStatus.Open : existingBreach.Status
                };

                await _breachRepository.SaveBreachAsync(existingBreach, ct);
            }
        }

        _logger.LogInformation("Detected {Count} DQ breaches", breaches.Count);
        return breaches;
    }

    public Task<List<DqBreach>> GetOpenBreachesAsync(string? datasetId = null, CancellationToken ct = default)
    {
        return _breachRepository.GetOpenBreachesAsync(datasetId, ct);
    }

    public async Task AcknowledgeBreachAsync(string breachId, string acknowledgedBy, CancellationToken ct = default)
    {
        var breach = await _breachRepository.GetBreachAsync(breachId, ct);
        if (breach == null)
        {
            throw new InvalidOperationException($"Breach {breachId} not found");
        }

        breach = breach with
        {
            Status = DqBreachStatus.Acknowledged
        };

        await _breachRepository.SaveBreachAsync(breach, ct);
        _logger.LogInformation("Breach {BreachId} acknowledged by {User}", breachId, acknowledgedBy);
    }

    public async Task ResolveBreachAsync(string breachId, string resolvedBy, string? notes = null, CancellationToken ct = default)
    {
        var breach = await _breachRepository.GetBreachAsync(breachId, ct);
        if (breach == null)
        {
            throw new InvalidOperationException($"Breach {breachId} not found");
        }

        breach = breach with
        {
            Status = DqBreachStatus.Resolved,
            ResolvedAt = DateTimeOffset.UtcNow,
            ResolvedBy = resolvedBy,
            RemediationNotes = notes
        };

        await _breachRepository.SaveBreachAsync(breach, ct);
        _logger.LogInformation("Breach {BreachId} resolved by {User}", breachId, resolvedBy);
    }

    public async Task<DqBreachStatistics> GetBreachStatisticsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null, CancellationToken ct = default)
    {
        var allBreaches = await _breachRepository.GetBreachesAsync(fromDate, toDate, ct);

        var totalBreaches = allBreaches.Count;
        var openBreaches = allBreaches.Count(b => b.Status == DqBreachStatus.Open || b.Status == DqBreachStatus.Acknowledged || b.Status == DqBreachStatus.InProgress);
        var resolvedBreaches = allBreaches.Count(b => b.Status == DqBreachStatus.Resolved);
        var exceptionBreaches = allBreaches.Count(b => b.Status == DqBreachStatus.Exception);

        var resolvedBreachesWithTime = allBreaches
            .Where(b => b.Status == DqBreachStatus.Resolved && b.ResolvedAt.HasValue)
            .ToList();

        var avgTimeToResolution = resolvedBreachesWithTime.Any()
            ? TimeSpan.FromMinutes(resolvedBreachesWithTime.Average(b => (b.ResolvedAt!.Value - b.DetectedAt).TotalMinutes))
            : TimeSpan.Zero;

        var breachesByDimension = allBreaches
            .GroupBy(b => b.Dimension)
            .ToDictionary(g => g.Key, g => g.Count());

        var breachesByDataset = allBreaches
            .GroupBy(b => b.DatasetId)
            .ToDictionary(g => g.Key, g => g.Count());

        return new DqBreachStatistics
        {
            TotalBreaches = totalBreaches,
            OpenBreaches = openBreaches,
            ResolvedBreaches = resolvedBreaches,
            ExceptionBreaches = exceptionBreaches,
            AverageTimeToResolution = avgTimeToResolution,
            BreachesByDimension = breachesByDimension,
            BreachesByDataset = breachesByDataset
        };
    }
}

/// <summary>
/// DQ breach repository interface
/// </summary>
public interface IDqBreachRepository
{
    Task<DqBreach?> GetBreachAsync(string breachId, CancellationToken ct = default);
    Task<List<DqBreach>> GetOpenBreachesAsync(string? datasetId, CancellationToken ct = default);
    Task<List<DqBreach>> GetBreachesAsync(DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct = default);
    Task<DqBreach?> GetOpenBreachByRuleAsync(string ruleId, CancellationToken ct = default);
    Task SaveBreachAsync(DqBreach breach, CancellationToken ct = default);
}


