using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Cutover service implementation
/// </summary>
public sealed class CutoverService : ICutoverService
{
    private readonly ICutoverRepository _repository;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<CutoverService> _logger;

    public CutoverService(
        ICutoverRepository repository,
        IFeatureFlagService featureFlagService,
        ILogger<CutoverService> logger)
    {
        _repository = repository;
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    public async Task<CutoverExecution> CreateCutoverAsync(CutoverExecution cutover, CancellationToken ct = default)
    {
        await _repository.SaveCutoverAsync(cutover, ct);
        _logger.LogInformation("Cutover created: {CutoverId} - {Name}", cutover.Id, cutover.Name);
        return cutover;
    }

    public async Task StartCutoverAsync(string cutoverId, CancellationToken ct = default)
    {
        var cutover = await _repository.GetCutoverAsync(cutoverId, ct);
        if (cutover == null)
        {
            throw new InvalidOperationException($"Cutover {cutoverId} not found");
        }

        // Validate readiness
        var readiness = await GetReadinessStatusAsync(ct);
        var criticalCriteria = readiness.Where(r => r.IsCritical && !r.IsMet).ToList();
        
        if (criticalCriteria.Any())
        {
            throw new InvalidOperationException($"Critical readiness criteria not met: {string.Join(", ", criticalCriteria.Select(r => r.Criterion))}");
        }

        cutover = cutover with
        {
            Phase = CutoverPhase.Execution,
            Status = CutoverStatus.InProgress,
            ActualStart = DateTimeOffset.UtcNow
        };

        await _repository.SaveCutoverAsync(cutover, ct);
        _logger.LogWarning("Cutover started: {CutoverId}", cutoverId);
    }

    public async Task CompletePhaseAsync(string cutoverId, CutoverPhase phase, CancellationToken ct = default)
    {
        var cutover = await _repository.GetCutoverAsync(cutoverId, ct);
        if (cutover == null)
        {
            throw new InvalidOperationException($"Cutover {cutoverId} not found");
        }

        var nextPhase = phase switch
        {
            CutoverPhase.Planning => CutoverPhase.Preparation,
            CutoverPhase.Preparation => CutoverPhase.DryRun,
            CutoverPhase.DryRun => CutoverPhase.Execution,
            CutoverPhase.Execution => CutoverPhase.Stabilization,
            CutoverPhase.Stabilization => CutoverPhase.Hypercare,
            CutoverPhase.Hypercare => CutoverPhase.Complete,
            _ => cutover.Phase
        };

        cutover = cutover with
        {
            Phase = nextPhase,
            Status = nextPhase == CutoverPhase.Complete ? CutoverStatus.Completed : cutover.Status
        };

        if (nextPhase == CutoverPhase.Complete)
        {
            cutover = cutover with { ActualEnd = DateTimeOffset.UtcNow };
        }

        await _repository.SaveCutoverAsync(cutover, ct);
        _logger.LogInformation("Cutover phase completed: {CutoverId} - Phase: {Phase}", cutoverId, nextPhase);
    }

    public Task<CutoverExecution?> GetCutoverAsync(string cutoverId, CancellationToken ct = default)
    {
        return _repository.GetCutoverAsync(cutoverId, ct);
    }

    public Task<List<ReadinessCriteria>> GetReadinessStatusAsync(CancellationToken ct = default)
    {
        return _repository.GetReadinessCriteriaAsync(ct);
    }

    public async Task ExecuteRollbackAsync(string cutoverId, string executedBy, CancellationToken ct = default)
    {
        var cutover = await _repository.GetCutoverAsync(cutoverId, ct);
        if (cutover == null)
        {
            throw new InvalidOperationException($"Cutover {cutoverId} not found");
        }

        var rollbackPlan = await _repository.GetRollbackPlanAsync(cutover.RollbackPlanId ?? "", ct);
        if (rollbackPlan == null)
        {
            throw new InvalidOperationException($"Rollback plan not found for cutover {cutoverId}");
        }

        _logger.LogWarning("Executing rollback for cutover {CutoverId}", cutoverId);

        // Disable feature flags
        var featureFlags = await _featureFlagService.GetEnabledFlagsAsync(ct);
        foreach (var flag in featureFlags)
        {
            await _featureFlagService.DisableFlagAsync(flag.Id, "rollback", ct);
        }

        // Execute rollback steps
        var updatedSteps = new List<RollbackStep>();
        foreach (var step in rollbackPlan.Steps.OrderBy(s => s.Order))
        {
            _logger.LogInformation("Executing rollback step {Order}: {Action}", step.Order, step.Action);
            
            // Execute rollback action (simplified)
            await Task.Delay(1000, ct);  // Simulate rollback action

            updatedSteps.Add(step with
            {
                IsCompleted = true,
                CompletedAt = DateTimeOffset.UtcNow,
                CompletedBy = executedBy
            });
        }

        rollbackPlan = rollbackPlan with
        {
            Status = RollbackStatus.Completed,
            ExecutedAt = DateTimeOffset.UtcNow,
            ExecutedBy = executedBy,
            Steps = updatedSteps
        };

        await _repository.SaveRollbackPlanAsync(rollbackPlan, ct);

        cutover = cutover with
        {
            Phase = CutoverPhase.Rollback,
            Status = CutoverStatus.RolledBack
        };

        await _repository.SaveCutoverAsync(cutover, ct);
        _logger.LogWarning("Rollback completed for cutover {CutoverId}", cutoverId);
    }
}

/// <summary>
/// Cutover repository interface
/// </summary>
public interface ICutoverRepository
{
    Task SaveCutoverAsync(CutoverExecution cutover, CancellationToken ct = default);
    Task<CutoverExecution?> GetCutoverAsync(string cutoverId, CancellationToken ct = default);
    Task<List<ReadinessCriteria>> GetReadinessCriteriaAsync(CancellationToken ct = default);
    Task SaveRollbackPlanAsync(RollbackPlan plan, CancellationToken ct = default);
    Task<RollbackPlan?> GetRollbackPlanAsync(string planId, CancellationToken ct = default);
}


