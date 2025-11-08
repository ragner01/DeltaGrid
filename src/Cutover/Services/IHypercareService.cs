using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Hypercare service for incident management during cutover
/// </summary>
public interface IHypercareService
{
    /// <summary>
    /// Report incident
    /// </summary>
    Task<HypercareIncident> ReportIncidentAsync(HypercareIncident incident, CancellationToken ct = default);

    /// <summary>
    /// Assign incident
    /// </summary>
    Task AssignIncidentAsync(string incidentId, string assignedTo, CancellationToken ct = default);

    /// <summary>
    /// Resolve incident
    /// </summary>
    Task ResolveIncidentAsync(string incidentId, string resolvedBy, string resolution, CancellationToken ct = default);

    /// <summary>
    /// Get open incidents
    /// </summary>
    Task<List<HypercareIncident>> GetOpenIncidentsAsync(string? cutoverId = null, IncidentSeverity? severity = null, CancellationToken ct = default);

    /// <summary>
    /// Get incident statistics
    /// </summary>
    Task<HypercareStatistics> GetStatisticsAsync(string cutoverId, CancellationToken ct = default);
}

/// <summary>
/// Hypercare statistics
/// </summary>
public sealed class HypercareStatistics
{
    public int TotalIncidents { get; init; }
    public int OpenIncidents { get; init; }
    public int ResolvedIncidents { get; init; }
    public Dictionary<IncidentSeverity, int> IncidentsBySeverity { get; init; } = new();
    public TimeSpan AverageResolutionTime { get; init; }
    public bool ZeroSev1Incidents { get; init; }
}


