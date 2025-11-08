using Microsoft.Extensions.Logging;

namespace IOC.Security.ThreatModel;

/// <summary>
/// Threat model registry for STRIDE analysis
/// </summary>
public interface IThreatModelRegistry
{
    /// <summary>
    /// Register a threat with mitigation
    /// </summary>
    Task RegisterThreatAsync(Threat threat, CancellationToken ct = default);

    /// <summary>
    /// Get all threats for a component
    /// </summary>
    Task<List<Threat>> GetThreatsAsync(string component, CancellationToken ct = default);

    /// <summary>
    /// Update threat mitigation status
    /// </summary>
    Task UpdateMitigationAsync(string threatId, MitigationStatus status, string? notes = null, CancellationToken ct = default);
}

/// <summary>
/// Threat definition with STRIDE classification
/// </summary>
public sealed class Threat
{
    public required string Id { get; init; }
    public required string Component { get; init; } // e.g., "API Gateway", "PTW Service"
    public required ThreatType Type { get; init; } // STRIDE classification
    public required string Description { get; init; }
    public required Severity Severity { get; init; }
    public required Mitigation Mitigation { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? MitigatedAt { get; init; }
}

/// <summary>
/// STRIDE threat types
/// </summary>
public enum ThreatType
{
    Spoofing,
    Tampering,
    Repudiation,
    InformationDisclosure,
    DenialOfService,
    ElevationOfPrivilege
}

/// <summary>
/// Threat severity
/// </summary>
public enum Severity
{
    Critical,
    High,
    Medium,
    Low
}

/// <summary>
/// Mitigation definition
/// </summary>
public sealed class Mitigation
{
    public required string Description { get; init; }
    public required MitigationStatus Status { get; init; }
    public List<string> Controls { get; init; } = new(); // e.g., ["JWT validation", "Rate limiting"]
    public string? Notes { get; init; }
}

/// <summary>
/// Mitigation status
/// </summary>
public enum MitigationStatus
{
    NotStarted,
    InProgress,
    Mitigated,
    Accepted, // Risk accepted
    Rejected // Mitigation not viable
}

/// <summary>
/// In-memory threat model registry
/// </summary>
public sealed class InMemoryThreatModelRegistry : IThreatModelRegistry
{
    private readonly Dictionary<string, Threat> _threats = new();
    private readonly ILogger<InMemoryThreatModelRegistry> _logger;

    public InMemoryThreatModelRegistry(ILogger<InMemoryThreatModelRegistry> logger)
    {
        _logger = logger;
        SeedDefaultThreats();
    }

    public Task RegisterThreatAsync(Threat threat, CancellationToken ct = default)
    {
        _threats[threat.Id] = threat;
        _logger.LogInformation("Threat registered: {ThreatId} - {Component} - {Type}", threat.Id, threat.Component, threat.Type);
        return Task.CompletedTask;
    }

    public Task<List<Threat>> GetThreatsAsync(string component, CancellationToken ct = default)
    {
        return Task.FromResult(_threats.Values
            .Where(t => t.Component == component)
            .ToList());
    }

    public Task UpdateMitigationAsync(string threatId, MitigationStatus status, string? notes = null, CancellationToken ct = default)
    {
        if (_threats.TryGetValue(threatId, out var threat))
        {
            var updatedMitigation = new Mitigation
            {
                Description = threat.Mitigation.Description,
                Status = status,
                Notes = notes ?? threat.Mitigation.Notes,
                Controls = threat.Mitigation.Controls
            };

            var updatedThreat = new Threat
            {
                Id = threat.Id,
                Component = threat.Component,
                Type = threat.Type,
                Description = threat.Description,
                Severity = threat.Severity,
                Mitigation = updatedMitigation,
                CreatedAt = threat.CreatedAt,
                MitigatedAt = status == MitigationStatus.Mitigated ? DateTimeOffset.UtcNow : threat.MitigatedAt
            };

            _threats[threatId] = updatedThreat;
            _logger.LogInformation("Threat mitigation updated: {ThreatId} - {Status}", threatId, status);
        }
        return Task.CompletedTask;
    }

    private void SeedDefaultThreats()
    {
        // API Gateway threats
        RegisterThreatAsync(new Threat
        {
            Id = "threat-api-spoofing",
            Component = "API Gateway",
            Type = ThreatType.Spoofing,
            Description = "Attacker spoofs legitimate API requests",
            Severity = Severity.High,
            Mitigation = new Mitigation
            {
                Description = "JWT validation, API key authentication",
                Status = MitigationStatus.Mitigated,
                Controls = new List<string> { "JWT validation", "API key authentication", "Rate limiting" }
            }
        }).Wait();

        RegisterThreatAsync(new Threat
        {
            Id = "threat-api-dos",
            Component = "API Gateway",
            Type = ThreatType.DenialOfService,
            Description = "Attacker floods API with requests",
            Severity = Severity.High,
            Mitigation = new Mitigation
            {
                Description = "Rate limiting, DDoS protection, circuit breakers",
                Status = MitigationStatus.Mitigated,
                Controls = new List<string> { "Rate limiting", "Polly circuit breakers", "Azure DDoS protection" }
            }
        }).Wait();

        // PTW Service threats
        RegisterThreatAsync(new Threat
        {
            Id = "threat-ptw-tampering",
            Component = "PTW Service",
            Type = ThreatType.Tampering,
            Description = "Attacker modifies permit data after approval",
            Severity = Severity.Critical,
            Mitigation = new Mitigation
            {
                Description = "Hash chain for non-repudiation, immutable archive",
                Status = MitigationStatus.Mitigated,
                Controls = new List<string> { "Hash chain", "Immutable archive", "Signature verification" }
            }
        }).Wait();

        // Secrets management threats
        RegisterThreatAsync(new Threat
        {
            Id = "threat-secrets-disclosure",
            Component = "Secrets Management",
            Type = ThreatType.InformationDisclosure,
            Description = "Secrets exposed in code, logs, or configuration",
            Severity = Severity.Critical,
            Mitigation = new Mitigation
            {
                Description = "Azure Key Vault, managed identities, secret scanning",
                Status = MitigationStatus.Mitigated,
                Controls = new List<string> { "Azure Key Vault", "Managed identities", "CI secret scanning" }
            }
        }).Wait();
    }
}

