using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Feature flag service for progressive enablement
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Create feature flag
    /// </summary>
    Task<FeatureFlag> CreateFlagAsync(FeatureFlag flag, CancellationToken ct = default);

    /// <summary>
    /// Enable feature flag
    /// </summary>
    Task EnableFlagAsync(string flagId, string enabledBy, CancellationToken ct = default);

    /// <summary>
    /// Disable feature flag
    /// </summary>
    Task DisableFlagAsync(string flagId, string disabledBy, CancellationToken ct = default);

    /// <summary>
    /// Get feature flag
    /// </summary>
    Task<FeatureFlag?> GetFlagAsync(string flagId, CancellationToken ct = default);

    /// <summary>
    /// Get enabled flags
    /// </summary>
    Task<List<FeatureFlag>> GetEnabledFlagsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if feature is enabled for tenant/user
    /// </summary>
    Task<bool> IsEnabledAsync(string flagId, string? tenantId = null, string? userId = null, CancellationToken ct = default);
}


