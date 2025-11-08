using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data quality rule engine
/// </summary>
public interface IDqEngine
{
    /// <summary>
    /// Evaluate all active rules for a dataset
    /// </summary>
    Task<List<DqScore>> EvaluateDatasetAsync(string datasetId, CancellationToken ct = default);

    /// <summary>
    /// Evaluate a specific rule
    /// </summary>
    Task<DqScore> EvaluateRuleAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Evaluate all active rules
    /// </summary>
    Task<List<DqScore>> EvaluateAllRulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get DQ score for a dataset (aggregated)
    /// </summary>
    Task<Dictionary<DqDimension, double>> GetDatasetScoreAsync(string datasetId, CancellationToken ct = default);
}


