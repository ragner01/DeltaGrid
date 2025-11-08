using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data quality rule engine implementation
/// </summary>
public sealed class DqEngine : IDqEngine
{
    private readonly IDqRuleRepository _ruleRepository;
    private readonly IDqScoreRepository _scoreRepository;
    private readonly ILogger<DqEngine> _logger;
    private readonly IConfiguration _configuration;

    public DqEngine(
        IDqRuleRepository ruleRepository,
        IDqScoreRepository scoreRepository,
        ILogger<DqEngine> logger,
        IConfiguration configuration)
    {
        _ruleRepository = ruleRepository;
        _scoreRepository = scoreRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<DqScore>> EvaluateDatasetAsync(string datasetId, CancellationToken ct = default)
    {
        var rules = await _ruleRepository.GetActiveRulesByDatasetAsync(datasetId, ct);
        var scores = new List<DqScore>();

        foreach (var rule in rules)
        {
            var score = await EvaluateRuleAsync(rule, ct);
            scores.Add(score);
            await _scoreRepository.SaveScoreAsync(score, ct);
        }

        _logger.LogInformation("Evaluated {Count} rules for dataset {DatasetId}", scores.Count, datasetId);
        return scores;
    }

    public async Task<DqScore> EvaluateRuleAsync(string ruleId, CancellationToken ct = default)
    {
        var rule = await _ruleRepository.GetRuleAsync(ruleId, ct);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule {ruleId} not found");
        }

        return await EvaluateRuleAsync(rule, ct);
    }

    public async Task<List<DqScore>> EvaluateAllRulesAsync(CancellationToken ct = default)
    {
        var rules = await _ruleRepository.GetAllActiveRulesAsync(ct);
        var scores = new List<DqScore>();

        foreach (var rule in rules)
        {
            var score = await EvaluateRuleAsync(rule, ct);
            scores.Add(score);
            await _scoreRepository.SaveScoreAsync(score, ct);
        }

        _logger.LogInformation("Evaluated {Count} rules", scores.Count);
        return scores;
    }

    public async Task<Dictionary<DqDimension, double>> GetDatasetScoreAsync(string datasetId, CancellationToken ct = default)
    {
        var latestScores = await _scoreRepository.GetLatestScoresByDatasetAsync(datasetId, ct);
        
        var aggregated = latestScores
            .GroupBy(s => s.Dimension)
            .ToDictionary(g => g.Key, g => g.Average(s => s.Score));

        return aggregated;
    }

    private async Task<DqScore> EvaluateRuleAsync(DqRule rule, CancellationToken ct)
    {
        _logger.LogInformation("Evaluating rule {RuleId} for dataset {DatasetId}", rule.Id, rule.DatasetId);

        double score = 0;
        string? details = null;

        // Evaluate based on dimension
        switch (rule.Dimension)
        {
            case DqDimension.Completeness:
                score = await EvaluateCompletenessAsync(rule, ct);
                break;
            case DqDimension.Timeliness:
                score = await EvaluateTimelinessAsync(rule, ct);
                break;
            case DqDimension.Validity:
                score = await EvaluateValidityAsync(rule, ct);
                break;
            case DqDimension.Consistency:
                score = await EvaluateConsistencyAsync(rule, ct);
                break;
        }

        // Check threshold
        var passed = CheckThreshold(score, rule.Threshold, rule.Operator);

        var dqScore = new DqScore
        {
            RuleId = rule.Id,
            DatasetId = rule.DatasetId,
            Dimension = rule.Dimension,
            EvaluatedAt = DateTimeOffset.UtcNow,
            Score = score,
            Threshold = rule.Threshold,
            Passed = passed,
            Details = details
        };

        // Update rule last evaluated timestamp
        await _ruleRepository.UpdateLastEvaluatedAsync(rule.Id, dqScore.EvaluatedAt, ct);

        return dqScore;
    }

    private async Task<double> EvaluateCompletenessAsync(DqRule rule, CancellationToken ct)
    {
        // Calculate completeness: % of non-null values
        // In production: Execute SQL query like: SELECT COUNT(*) as total, COUNT(column) as non_null FROM table
        
        // Placeholder: simulate completeness calculation
        await Task.Delay(100, ct);
        
        // Example: 85% completeness
        var completeness = 85.0;
        
        _logger.LogDebug("Completeness for {RuleId}: {Score}%", rule.Id, completeness);
        return completeness;
    }

    private async Task<double> EvaluateTimelinessAsync(DqRule rule, CancellationToken ct)
    {
        // Calculate timeliness: minutes since last update
        // In production: Query last update timestamp from metadata
        
        await Task.Delay(100, ct);
        
        // Example: Last update was 30 minutes ago
        var minutesSinceUpdate = 30.0;
        
        _logger.LogDebug("Timeliness for {RuleId}: {Minutes} minutes", rule.Id, minutesSinceUpdate);
        return minutesSinceUpdate;
    }

    private async Task<double> EvaluateValidityAsync(DqRule rule, CancellationToken ct)
    {
        // Calculate validity: % of records matching format/rules
        // In production: Execute validation query based on expression
        
        await Task.Delay(100, ct);
        
        // Example: 92% of records are valid
        var validity = 92.0;
        
        _logger.LogDebug("Validity for {RuleId}: {Score}%", rule.Id, validity);
        return validity;
    }

    private async Task<double> EvaluateConsistencyAsync(DqRule rule, CancellationToken ct)
    {
        // Calculate consistency: % of records consistent across fields/datasets
        // In production: Execute cross-validation query
        
        await Task.Delay(100, ct);
        
        // Example: 88% consistency
        var consistency = 88.0;
        
        _logger.LogDebug("Consistency for {RuleId}: {Score}%", rule.Id, consistency);
        return consistency;
    }

    private bool CheckThreshold(double score, double threshold, DqThresholdOperator op)
    {
        return op switch
        {
            DqThresholdOperator.GreaterThan => score > threshold,
            DqThresholdOperator.GreaterThanOrEqual => score >= threshold,
            DqThresholdOperator.LessThan => score < threshold,
            DqThresholdOperator.LessThanOrEqual => score <= threshold,
            DqThresholdOperator.Equal => Math.Abs(score - threshold) < 0.001,
            DqThresholdOperator.NotEqual => Math.Abs(score - threshold) >= 0.001,
            _ => false
        };
    }
}

/// <summary>
/// DQ rule repository interface
/// </summary>
public interface IDqRuleRepository
{
    Task<DqRule?> GetRuleAsync(string ruleId, CancellationToken ct = default);
    Task<List<DqRule>> GetAllActiveRulesAsync(CancellationToken ct = default);
    Task<List<DqRule>> GetActiveRulesByDatasetAsync(string datasetId, CancellationToken ct = default);
    Task SaveRuleAsync(DqRule rule, CancellationToken ct = default);
    Task UpdateLastEvaluatedAsync(string ruleId, DateTimeOffset evaluatedAt, CancellationToken ct = default);
}

/// <summary>
/// DQ score repository interface
/// </summary>
public interface IDqScoreRepository
{
    Task SaveScoreAsync(DqScore score, CancellationToken ct = default);
    Task<List<DqScore>> GetLatestScoresByDatasetAsync(string datasetId, CancellationToken ct = default);
    Task<List<DqScore>> GetScoresByRuleAsync(string ruleId, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct = default);
    Task<DqScore?> GetLatestScoreByRuleAsync(string ruleId, CancellationToken ct = default);
}

