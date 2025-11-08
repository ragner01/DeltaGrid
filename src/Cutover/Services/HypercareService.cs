using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Hypercare service implementation
/// </summary>
public sealed class HypercareService : IHypercareService
{
    private readonly IHypercareRepository _repository;
    private readonly ILogger<HypercareService> _logger;

    public HypercareService(
        IHypercareRepository repository,
        ILogger<HypercareService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<HypercareIncident> ReportIncidentAsync(HypercareIncident incident, CancellationToken ct = default)
    {
        await _repository.SaveIncidentAsync(incident, ct);
        _logger.LogWarning("Hypercare incident reported: {IncidentId} - {Severity} - {Title}", 
            incident.Id, incident.Severity, incident.Title);
        return incident;
    }

    public async Task AssignIncidentAsync(string incidentId, string assignedTo, CancellationToken ct = default)
    {
        var incident = await _repository.GetIncidentAsync(incidentId, ct);
        if (incident == null)
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        incident = incident with
        {
            Status = HypercareIncidentStatus.Assigned,
            AssignedTo = assignedTo
        };

        await _repository.SaveIncidentAsync(incident, ct);
        _logger.LogInformation("Incident {IncidentId} assigned to {User}", incidentId, assignedTo);
    }

    public async Task ResolveIncidentAsync(string incidentId, string resolvedBy, string resolution, CancellationToken ct = default)
    {
        var incident = await _repository.GetIncidentAsync(incidentId, ct);
        if (incident == null)
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        incident = incident with
        {
            Status = HypercareIncidentStatus.Resolved,
            ResolvedAt = DateTimeOffset.UtcNow,
            Resolution = resolution
        };

        await _repository.SaveIncidentAsync(incident, ct);
        _logger.LogInformation("Incident {IncidentId} resolved by {User}", incidentId, resolvedBy);
    }

    public Task<List<HypercareIncident>> GetOpenIncidentsAsync(string? cutoverId = null, IncidentSeverity? severity = null, CancellationToken ct = default)
    {
        return _repository.GetOpenIncidentsAsync(cutoverId, severity, ct);
    }

    public async Task<HypercareStatistics> GetStatisticsAsync(string cutoverId, CancellationToken ct = default)
    {
        var incidents = await _repository.GetIncidentsAsync(cutoverId, ct);

        var totalIncidents = incidents.Count;
        var openIncidents = incidents.Count(i => i.Status != HypercareIncidentStatus.Resolved && i.Status != HypercareIncidentStatus.Closed);
        var resolvedIncidents = incidents.Count(i => i.Status == HypercareIncidentStatus.Resolved || i.Status == HypercareIncidentStatus.Closed);

        var incidentsBySeverity = incidents.GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        var resolvedIncidentsWithTime = incidents
            .Where(i => i.ResolvedAt.HasValue)
            .ToList();

        var avgResolutionTime = resolvedIncidentsWithTime.Any()
            ? TimeSpan.FromMinutes(resolvedIncidentsWithTime.Average(i => (i.ResolvedAt!.Value - i.ReportedAt).TotalMinutes))
            : TimeSpan.Zero;

        var zeroSev1Incidents = !incidents.Any(i => i.Severity == IncidentSeverity.Sev1);

        return new HypercareStatistics
        {
            TotalIncidents = totalIncidents,
            OpenIncidents = openIncidents,
            ResolvedIncidents = resolvedIncidents,
            IncidentsBySeverity = incidentsBySeverity,
            AverageResolutionTime = avgResolutionTime,
            ZeroSev1Incidents = zeroSev1Incidents
        };
    }
}

/// <summary>
/// Hypercare repository interface
/// </summary>
public interface IHypercareRepository
{
    Task SaveIncidentAsync(HypercareIncident incident, CancellationToken ct = default);
    Task<HypercareIncident?> GetIncidentAsync(string incidentId, CancellationToken ct = default);
    Task<List<HypercareIncident>> GetOpenIncidentsAsync(string? cutoverId, IncidentSeverity? severity, CancellationToken ct = default);
    Task<List<HypercareIncident>> GetIncidentsAsync(string cutoverId, CancellationToken ct = default);
}


