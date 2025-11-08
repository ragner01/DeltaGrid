using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data lineage service for impact assessment
/// </summary>
public interface ILineageService
{
    /// <summary>
    /// Assess impact of DQ breach
    /// </summary>
    Task<ImpactAssessment> AssessImpactAsync(string breachId, CancellationToken ct = default);

    /// <summary>
    /// Get lineage for a dataset
    /// </summary>
    Task<List<DataLineage>> GetLineageAsync(string datasetId, CancellationToken ct = default);

    /// <summary>
    /// Get downstream dependencies
    /// </summary>
    Task<List<string>> GetDownstreamDatasetsAsync(string datasetId, CancellationToken ct = default);

    /// <summary>
    /// Record lineage
    /// </summary>
    Task RecordLineageAsync(DataLineage lineage, CancellationToken ct = default);
}


