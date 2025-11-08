using IOC.DataGovernance.Models;
using IOC.DataGovernance.Services;

namespace IOC.DataGovernance.Persistence;

/// <summary>
/// In-memory implementation of governance repositories
/// </summary>
public sealed class InMemoryGovernanceRepository :
    IDqRuleRepository,
    IDqScoreRepository,
    IDqBreachRepository,
    IDqExceptionRepository,
    IAccessRequestRepository,
    ILineageRepository,
    IDatasetMetadataRepository
{
    private readonly Dictionary<string, DqRule> _rules = new();
    private readonly Dictionary<string, DqScore> _scores = new();
    private readonly Dictionary<string, DqBreach> _breaches = new();
    private readonly Dictionary<string, DqException> _exceptions = new();
    private readonly Dictionary<string, AccessRequest> _accessRequests = new();
    private readonly Dictionary<string, DataLineage> _lineage = new();
    private readonly Dictionary<string, ImpactAssessment> _impactAssessments = new();
    private readonly Dictionary<string, DatasetMetadata> _datasets = new();

    // IDqRuleRepository
    public Task<DqRule?> GetRuleAsync(string ruleId, CancellationToken ct = default)
    {
        return Task.FromResult(_rules.TryGetValue(ruleId, out var rule) ? rule : null);
    }

    public Task<List<DqRule>> GetAllActiveRulesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_rules.Values.Where(r => r.IsActive).ToList());
    }

    public Task<List<DqRule>> GetActiveRulesByDatasetAsync(string datasetId, CancellationToken ct = default)
    {
        return Task.FromResult(_rules.Values.Where(r => r.IsActive && r.DatasetId == datasetId).ToList());
    }

    public Task SaveRuleAsync(DqRule rule, CancellationToken ct = default)
    {
        _rules[rule.Id] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateLastEvaluatedAsync(string ruleId, DateTimeOffset evaluatedAt, CancellationToken ct = default)
    {
        if (_rules.TryGetValue(ruleId, out var rule))
        {
            _rules[ruleId] = rule with { LastEvaluatedAt = evaluatedAt };
        }
        return Task.CompletedTask;
    }

    // IDqScoreRepository
    public Task SaveScoreAsync(DqScore score, CancellationToken ct = default)
    {
        _scores[$"{score.RuleId}:{score.EvaluatedAt:O}"] = score;
        return Task.CompletedTask;
    }

    public Task<List<DqScore>> GetLatestScoresByDatasetAsync(string datasetId, CancellationToken ct = default)
    {
        var scores = _scores.Values
            .Where(s => s.DatasetId == datasetId)
            .GroupBy(s => s.RuleId)
            .Select(g => g.OrderByDescending(s => s.EvaluatedAt).First())
            .ToList();

        return Task.FromResult(scores);
    }

    public Task<List<DqScore>> GetScoresByRuleAsync(string ruleId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct = default)
    {
        var scores = _scores.Values.Where(s => s.RuleId == ruleId);

        if (fromDate.HasValue)
        {
            scores = scores.Where(s => s.EvaluatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            scores = scores.Where(s => s.EvaluatedAt <= toDate.Value);
        }

        return Task.FromResult(scores.OrderByDescending(s => s.EvaluatedAt).ToList());
    }

    public Task<DqScore?> GetLatestScoreByRuleAsync(string ruleId, CancellationToken ct = default)
    {
        var score = _scores.Values
            .Where(s => s.RuleId == ruleId)
            .OrderByDescending(s => s.EvaluatedAt)
            .FirstOrDefault();

        return Task.FromResult(score);
    }

    // IDqBreachRepository
    public Task<DqBreach?> GetBreachAsync(string breachId, CancellationToken ct = default)
    {
        return Task.FromResult(_breaches.TryGetValue(breachId, out var breach) ? breach : null);
    }

    public Task<List<DqBreach>> GetOpenBreachesAsync(string? datasetId, CancellationToken ct = default)
    {
        var breaches = _breaches.Values.Where(b =>
            (b.Status == DqBreachStatus.Open || b.Status == DqBreachStatus.Acknowledged || b.Status == DqBreachStatus.InProgress) &&
            (datasetId == null || b.DatasetId == datasetId));

        return Task.FromResult(breaches.OrderByDescending(b => b.DetectedAt).ToList());
    }

    public Task<List<DqBreach>> GetBreachesAsync(DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct = default)
    {
        var breaches = _breaches.Values.AsEnumerable();

        if (fromDate.HasValue)
        {
            breaches = breaches.Where(b => b.DetectedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            breaches = breaches.Where(b => b.DetectedAt <= toDate.Value);
        }

        return Task.FromResult(breaches.OrderByDescending(b => b.DetectedAt).ToList());
    }

    public Task<DqBreach?> GetOpenBreachByRuleAsync(string ruleId, CancellationToken ct = default)
    {
        var breach = _breaches.Values
            .Where(b => b.RuleId == ruleId && (b.Status == DqBreachStatus.Open || b.Status == DqBreachStatus.Acknowledged || b.Status == DqBreachStatus.InProgress))
            .OrderByDescending(b => b.DetectedAt)
            .FirstOrDefault();

        return Task.FromResult(breach);
    }

    public Task SaveBreachAsync(DqBreach breach, CancellationToken ct = default)
    {
        _breaches[breach.Id] = breach;
        return Task.CompletedTask;
    }

    // IDqExceptionRepository
    public Task<DqException?> GetExceptionAsync(string exceptionId, CancellationToken ct = default)
    {
        return Task.FromResult(_exceptions.TryGetValue(exceptionId, out var exception) ? exception : null);
    }

    public Task<List<DqException>> GetPendingExceptionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_exceptions.Values.Where(e => e.Status == DqExceptionStatus.Pending).ToList());
    }

    public Task<List<DqException>> GetApprovedExceptionsWithExpiryAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_exceptions.Values
            .Where(e => e.Status == DqExceptionStatus.Approved && e.ExpiresAt.HasValue)
            .ToList());
    }

    public Task SaveExceptionAsync(DqException exception, CancellationToken ct = default)
    {
        _exceptions[exception.Id] = exception;
        return Task.CompletedTask;
    }

    // IAccessRequestRepository
    public Task<AccessRequest?> GetRequestAsync(string requestId, CancellationToken ct = default)
    {
        return Task.FromResult(_accessRequests.TryGetValue(requestId, out var request) ? request : null);
    }

    public Task<List<AccessRequest>> GetPendingRequestsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_accessRequests.Values.Where(r => r.Status == AccessRequestStatus.Pending).ToList());
    }

    public Task<List<AccessRequest>> GetApprovedRequestsWithExpiryAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_accessRequests.Values
            .Where(r => r.Status == AccessRequestStatus.Approved && r.ExpiresAt.HasValue)
            .ToList());
    }

    public Task<List<AccessRequest>> GetRequestHistoryAsync(string? datasetId, string? requestedBy, CancellationToken ct = default)
    {
        var requests = _accessRequests.Values.AsEnumerable();

        if (datasetId != null)
        {
            requests = requests.Where(r => r.DatasetId == datasetId);
        }

        if (requestedBy != null)
        {
            requests = requests.Where(r => r.RequestedBy == requestedBy);
        }

        return Task.FromResult(requests.OrderByDescending(r => r.RequestedAt).ToList());
    }

    public Task SaveRequestAsync(AccessRequest request, CancellationToken ct = default)
    {
        _accessRequests[request.Id] = request;
        return Task.CompletedTask;
    }

    // ILineageRepository
    public Task<List<DataLineage>> GetLineageBySourceAsync(string sourceId, CancellationToken ct = default)
    {
        return Task.FromResult(_lineage.Values.Where(l => l.SourceId == sourceId).ToList());
    }

    public Task<List<DataLineage>> GetLineageByTargetAsync(string targetId, CancellationToken ct = default)
    {
        return Task.FromResult(_lineage.Values.Where(l => l.TargetId == targetId).ToList());
    }

    public Task SaveLineageAsync(DataLineage lineage, CancellationToken ct = default)
    {
        _lineage[lineage.Id] = lineage;
        return Task.CompletedTask;
    }

    public Task SaveImpactAssessmentAsync(ImpactAssessment assessment, CancellationToken ct = default)
    {
        _impactAssessments[assessment.Id] = assessment;
        return Task.CompletedTask;
    }

    // IDatasetMetadataRepository
    public Task<List<DatasetMetadata>> GetDatasetsByOwnerAsync(string owner, CancellationToken ct = default)
    {
        return Task.FromResult(_datasets.Values.Where(d => d.Owner == owner).ToList());
    }

    public Task<DatasetMetadata?> GetDatasetAsync(string datasetId, CancellationToken ct = default)
    {
        return Task.FromResult(_datasets.TryGetValue(datasetId, out var dataset) ? dataset : null);
    }

    public Task SaveDatasetAsync(DatasetMetadata dataset, CancellationToken ct = default)
    {
        _datasets[dataset.Id] = dataset;
        return Task.CompletedTask;
    }

    // Seed default data
    public void SeedData()
    {
        // Seed sample datasets
        _datasets["wells"] = new DatasetMetadata
        {
            Id = "wells",
            Name = "Wells",
            Description = "Well data",
            Owner = "production-engineer",
            Classification = DatasetClassification.Internal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _datasets["allocation"] = new DatasetMetadata
        {
            Id = "allocation",
            Name = "Allocation Results",
            Description = "Production allocation results",
            Owner = "production-engineer",
            Classification = DatasetClassification.Internal,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Seed sample rules
        _rules["completeness-wells"] = new DqRule
        {
            Id = "completeness-wells",
            Name = "Wells Completeness",
            DatasetId = "wells",
            Dimension = DqDimension.Completeness,
            Expression = "COUNT(*) / COUNT(well_id) * 100",
            Threshold = 95.0,
            Operator = DqThresholdOperator.GreaterThanOrEqual,
            Description = "Well completeness must be >= 95%",
            Owner = "production-engineer",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _rules["timeliness-allocation"] = new DqRule
        {
            Id = "timeliness-allocation",
            Name = "Allocation Timeliness",
            DatasetId = "allocation",
            Dimension = DqDimension.Timeliness,
            Expression = "DATEDIFF(MINUTE, last_update, GETDATE())",
            Threshold = 60.0,
            Operator = DqThresholdOperator.LessThanOrEqual,
            Description = "Allocation data must be updated within 60 minutes",
            Owner = "production-engineer",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}


