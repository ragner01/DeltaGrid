using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Feature flag service implementation
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagRepository _repository;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(
        IFeatureFlagRepository repository,
        ILogger<FeatureFlagService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FeatureFlag> CreateFlagAsync(FeatureFlag flag, CancellationToken ct = default)
    {
        await _repository.SaveFlagAsync(flag, ct);
        _logger.LogInformation("Feature flag created: {FlagId} - {Name}", flag.Id, flag.Name);
        return flag;
    }

    public async Task EnableFlagAsync(string flagId, string enabledBy, CancellationToken ct = default)
    {
        var flag = await _repository.GetFlagAsync(flagId, ct);
        if (flag == null)
        {
            throw new InvalidOperationException($"Feature flag {flagId} not found");
        }

        flag = flag with
        {
            IsEnabled = true,
            EnabledAt = DateTimeOffset.UtcNow
        };

        await _repository.SaveFlagAsync(flag, ct);
        _logger.LogInformation("Feature flag enabled: {FlagId} by {User}", flagId, enabledBy);
    }

    public async Task DisableFlagAsync(string flagId, string disabledBy, CancellationToken ct = default)
    {
        var flag = await _repository.GetFlagAsync(flagId, ct);
        if (flag == null)
        {
            throw new InvalidOperationException($"Feature flag {flagId} not found");
        }

        flag = flag with
        {
            IsEnabled = false,
            EnabledAt = null
        };

        await _repository.SaveFlagAsync(flag, ct);
        _logger.LogInformation("Feature flag disabled: {FlagId} by {User}", flagId, disabledBy);
    }

    public Task<FeatureFlag?> GetFlagAsync(string flagId, CancellationToken ct = default)
    {
        return _repository.GetFlagAsync(flagId, ct);
    }

    public Task<List<FeatureFlag>> GetEnabledFlagsAsync(CancellationToken ct = default)
    {
        return _repository.GetEnabledFlagsAsync(ct);
    }

    public async Task<bool> IsEnabledAsync(string flagId, string? tenantId = null, string? userId = null, CancellationToken ct = default)
    {
        var flag = await _repository.GetFlagAsync(flagId, ct);
        if (flag == null || !flag.IsEnabled)
        {
            return false;
        }

        return flag.Strategy switch
        {
            FeatureFlagStrategy.AllOrNothing => flag.IsEnabled,
            FeatureFlagStrategy.TenantBased => tenantId != null && flag.EnabledTenants.Contains(tenantId),
            FeatureFlagStrategy.UserBased => userId != null && flag.EnabledUsers.Contains(userId),
            FeatureFlagStrategy.Progressive => CheckProgressiveRollout(flag, tenantId, userId),
            _ => false
        };
    }

    private bool CheckProgressiveRollout(FeatureFlag flag, string? tenantId, string? userId)
    {
        // Progressive rollout based on percentage
        // Simplified: use hash of tenant/user ID to determine if enabled
        var hashTarget = tenantId ?? userId ?? "";
        var hash = hashTarget.GetHashCode();
        var percentage = Math.Abs(hash % 100);
        
        return percentage < flag.RolloutPercentage;
    }
}

/// <summary>
/// Feature flag repository interface
/// </summary>
public interface IFeatureFlagRepository
{
    Task SaveFlagAsync(FeatureFlag flag, CancellationToken ct = default);
    Task<FeatureFlag?> GetFlagAsync(string flagId, CancellationToken ct = default);
    Task<List<FeatureFlag>> GetEnabledFlagsAsync(CancellationToken ct = default);
    Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken ct = default);
}


