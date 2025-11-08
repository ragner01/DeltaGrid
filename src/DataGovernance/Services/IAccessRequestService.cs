using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Access request service
/// </summary>
public interface IAccessRequestService
{
    /// <summary>
    /// Create access request
    /// </summary>
    Task<AccessRequest> CreateRequestAsync(AccessRequest request, CancellationToken ct = default);

    /// <summary>
    /// Approve access request
    /// </summary>
    Task ApproveRequestAsync(string requestId, string approvedBy, CancellationToken ct = default);

    /// <summary>
    /// Reject access request
    /// </summary>
    Task RejectRequestAsync(string requestId, string rejectedBy, string reason, CancellationToken ct = default);

    /// <summary>
    /// Revoke access
    /// </summary>
    Task RevokeAccessAsync(string requestId, string revokedBy, CancellationToken ct = default);

    /// <summary>
    /// Get pending requests
    /// </summary>
    Task<List<AccessRequest>> GetPendingRequestsAsync(CancellationToken ct = default);

    /// <summary>
    /// Check expired access and auto-expire
    /// </summary>
    Task<List<AccessRequest>> ExpireAccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Get access request history
    /// </summary>
    Task<List<AccessRequest>> GetRequestHistoryAsync(string? datasetId = null, string? requestedBy = null, CancellationToken ct = default);
}


