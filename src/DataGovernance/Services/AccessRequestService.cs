using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// Access request service implementation
/// </summary>
public sealed class AccessRequestService : IAccessRequestService
{
    private readonly IAccessRequestRepository _repository;
    private readonly ILogger<AccessRequestService> _logger;

    public AccessRequestService(
        IAccessRequestRepository repository,
        ILogger<AccessRequestService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AccessRequest> CreateRequestAsync(AccessRequest request, CancellationToken ct = default)
    {
        await _repository.SaveRequestAsync(request, ct);
        _logger.LogInformation("Access request created: {RequestId} for dataset {DatasetId} by {User}",
            request.Id, request.DatasetId, request.RequestedBy);
        return request;
    }

    public async Task ApproveRequestAsync(string requestId, string approvedBy, CancellationToken ct = default)
    {
        var request = await _repository.GetRequestAsync(requestId, ct);
        if (request == null)
        {
            throw new InvalidOperationException($"Access request {requestId} not found");
        }

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Access request {requestId} is not pending");
        }

        request = request with
        {
            Status = AccessRequestStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedBy = approvedBy
        };

        await _repository.SaveRequestAsync(request, ct);
        _logger.LogInformation("Access request {RequestId} approved by {User}", requestId, approvedBy);
    }

    public async Task RejectRequestAsync(string requestId, string rejectedBy, string reason, CancellationToken ct = default)
    {
        var request = await _repository.GetRequestAsync(requestId, ct);
        if (request == null)
        {
            throw new InvalidOperationException($"Access request {requestId} not found");
        }

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Access request {requestId} is not pending");
        }

        request = request with
        {
            Status = AccessRequestStatus.Rejected,
            RejectionReason = reason
        };

        await _repository.SaveRequestAsync(request, ct);
        _logger.LogInformation("Access request {RequestId} rejected by {User}: {Reason}", requestId, rejectedBy, reason);
    }

    public async Task RevokeAccessAsync(string requestId, string revokedBy, CancellationToken ct = default)
    {
        var request = await _repository.GetRequestAsync(requestId, ct);
        if (request == null)
        {
            throw new InvalidOperationException($"Access request {requestId} not found");
        }

        if (request.Status != AccessRequestStatus.Approved)
        {
            throw new InvalidOperationException($"Access request {requestId} is not approved");
        }

        request = request with
        {
            Status = AccessRequestStatus.Revoked
        };

        await _repository.SaveRequestAsync(request, ct);
        _logger.LogWarning("Access request {RequestId} revoked by {User}", requestId, revokedBy);
    }

    public Task<List<AccessRequest>> GetPendingRequestsAsync(CancellationToken ct = default)
    {
        return _repository.GetPendingRequestsAsync(ct);
    }

    public async Task<List<AccessRequest>> ExpireAccessAsync(CancellationToken ct = default)
    {
        var requests = await _repository.GetApprovedRequestsWithExpiryAsync(ct);
        var expired = new List<AccessRequest>();

        foreach (var request in requests)
        {
            if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                var updatedRequest = request with
                {
                    Status = AccessRequestStatus.Expired
                };

                await _repository.SaveRequestAsync(updatedRequest, ct);
                expired.Add(updatedRequest);

                _logger.LogInformation("Access request {RequestId} expired automatically", request.Id);
            }
        }

        return expired;
    }

    public Task<List<AccessRequest>> GetRequestHistoryAsync(string? datasetId = null, string? requestedBy = null, CancellationToken ct = default)
    {
        return _repository.GetRequestHistoryAsync(datasetId, requestedBy, ct);
    }
}

/// <summary>
/// Access request repository interface
/// </summary>
public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetRequestAsync(string requestId, CancellationToken ct = default);
    Task<List<AccessRequest>> GetPendingRequestsAsync(CancellationToken ct = default);
    Task<List<AccessRequest>> GetApprovedRequestsWithExpiryAsync(CancellationToken ct = default);
    Task<List<AccessRequest>> GetRequestHistoryAsync(string? datasetId, string? requestedBy, CancellationToken ct = default);
    Task SaveRequestAsync(AccessRequest request, CancellationToken ct = default);
}


