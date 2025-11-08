using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Cutover management service
/// </summary>
public interface ICutoverService
{
    /// <summary>
    /// Create cutover execution
    /// </summary>
    Task<CutoverExecution> CreateCutoverAsync(CutoverExecution cutover, CancellationToken ct = default);

    /// <summary>
    /// Start cutover
    /// </summary>
    Task StartCutoverAsync(string cutoverId, CancellationToken ct = default);

    /// <summary>
    /// Complete cutover phase
    /// </summary>
    Task CompletePhaseAsync(string cutoverId, CutoverPhase phase, CancellationToken ct = default);

    /// <summary>
    /// Get cutover execution
    /// </summary>
    Task<CutoverExecution?> GetCutoverAsync(string cutoverId, CancellationToken ct = default);

    /// <summary>
    /// Get readiness status
    /// </summary>
    Task<List<ReadinessCriteria>> GetReadinessStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Execute rollback
    /// </summary>
    Task ExecuteRollbackAsync(string cutoverId, string executedBy, CancellationToken ct = default);
}


