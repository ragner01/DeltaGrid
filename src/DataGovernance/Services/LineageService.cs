using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Data lineage service implementation
/// </summary>
public sealed class LineageService : ILineageService
{
    private readonly ILineageRepository _lineageRepository;
    private readonly IDqBreachRepository _breachRepository;
    private readonly ILogger<LineageService> _logger;

    public LineageService(
        ILineageRepository lineageRepository,
        IDqBreachRepository breachRepository,
        ILogger<LineageService> logger)
    {
        _lineageRepository = lineageRepository;
        _breachRepository = breachRepository;
        _logger = logger;
    }

    public async Task<ImpactAssessment> AssessImpactAsync(string breachId, CancellationToken ct = default)
    {
        var breach = await _breachRepository.GetBreachAsync(breachId, ct);
        if (breach == null)
        {
            throw new InvalidOperationException($"Breach {breachId} not found");
        }

        _logger.LogInformation("Assessing impact of breach {BreachId} on dataset {DatasetId}", breachId, breach.DatasetId);

        // Get downstream datasets
        var downstreamDatasets = await GetDownstreamDatasetsAsync(breach.DatasetId, ct);

        // Identify affected reports (simplified)
        var affectedReports = downstreamDatasets
            .SelectMany(d => GetReportsForDataset(d))
            .Distinct()
            .ToList();

        // Identify affected services (simplified)
        var affectedServices = downstreamDatasets
            .SelectMany(d => GetServicesForDataset(d))
            .Distinct()
            .ToList();

        // Determine severity
        var severity = DetermineSeverity(breach.Dimension, affectedReports.Count, affectedServices.Count);

        var assessment = new ImpactAssessment
        {
            Id = Guid.NewGuid().ToString(),
            SourceBreachId = breachId,
            AffectedDatasets = downstreamDatasets,
            AffectedReports = affectedReports,
            AffectedServices = affectedServices,
            AssessedAt = DateTimeOffset.UtcNow,
            Severity = severity,
            Recommendations = GenerateRecommendations(severity, breach.Dimension)
        };

        await _lineageRepository.SaveImpactAssessmentAsync(assessment, ct);

        _logger.LogInformation("Impact assessment completed: {Severity} severity, {DatasetCount} datasets, {ReportCount} reports, {ServiceCount} services",
            severity, downstreamDatasets.Count, affectedReports.Count, affectedServices.Count);

        return assessment;
    }

    public Task<List<DataLineage>> GetLineageAsync(string datasetId, CancellationToken ct = default)
    {
        return _lineageRepository.GetLineageByTargetAsync(datasetId, ct);
    }

    public async Task<List<string>> GetDownstreamDatasetsAsync(string datasetId, CancellationToken ct = default)
    {
        var lineage = await _lineageRepository.GetLineageBySourceAsync(datasetId, ct);
        var downstream = new HashSet<string>();

        foreach (var lin in lineage)
        {
            downstream.Add(lin.TargetId);
            // Recursively get downstream of downstream
            var nested = await GetDownstreamDatasetsAsync(lin.TargetId, ct);
            foreach (var nestedDataset in nested)
            {
                downstream.Add(nestedDataset);
            }
        }

        return downstream.ToList();
    }

    public Task RecordLineageAsync(DataLineage lineage, CancellationToken ct = default)
    {
        return _lineageRepository.SaveLineageAsync(lineage, ct);
    }

    private List<string> GetReportsForDataset(string datasetId)
    {
        // In production: Query report metadata for datasets used
        // Placeholder: return sample reports
        return new List<string> { $"report-{datasetId}", $"dashboard-{datasetId}" };
    }

    private List<string> GetServicesForDataset(string datasetId)
    {
        // In production: Query service metadata for datasets consumed
        // Placeholder: return sample services
        return new List<string> { $"service-{datasetId}" };
    }

    private ImpactSeverity DetermineSeverity(DqDimension dimension, int reportCount, int serviceCount)
    {
        var impactScore = reportCount + serviceCount * 2;

        if (dimension == DqDimension.Consistency && impactScore >= 10)
        {
            return ImpactSeverity.Critical;
        }

        if (impactScore >= 10)
        {
            return ImpactSeverity.High;
        }

        if (impactScore >= 5)
        {
            return ImpactSeverity.Medium;
        }

        return ImpactSeverity.Low;
    }

    private string? GenerateRecommendations(ImpactSeverity severity, DqDimension dimension)
    {
        return severity switch
        {
            ImpactSeverity.Critical => "Immediate remediation required. Consider blocking downstream processes.",
            ImpactSeverity.High => "Priority remediation required. Notify affected stakeholders.",
            ImpactSeverity.Medium => "Remediation recommended. Monitor downstream systems.",
            ImpactSeverity.Low => "Remediation recommended when convenient.",
            _ => null
        };
    }
}

/// <summary>
/// Lineage repository interface
/// </summary>
public interface ILineageRepository
{
    Task<List<DataLineage>> GetLineageBySourceAsync(string sourceId, CancellationToken ct = default);
    Task<List<DataLineage>> GetLineageByTargetAsync(string targetId, CancellationToken ct = default);
    Task SaveLineageAsync(DataLineage lineage, CancellationToken ct = default);
    Task SaveImpactAssessmentAsync(ImpactAssessment assessment, CancellationToken ct = default);
}


