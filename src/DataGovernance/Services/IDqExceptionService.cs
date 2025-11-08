using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// DQ exception service
/// </summary>
public interface IDqExceptionService
{
    /// <summary>
    /// Request exception for breach
    /// </summary>
    Task<DqException> RequestExceptionAsync(DqException exception, CancellationToken ct = default);

    /// <summary>
    /// Approve exception
    /// </summary>
    Task ApproveExceptionAsync(string exceptionId, string approvedBy, CancellationToken ct = default);

    /// <summary>
    /// Reject exception
    /// </summary>
    Task RejectExceptionAsync(string exceptionId, string rejectedBy, string reason, CancellationToken ct = default);

    /// <summary>
    /// Check expired exceptions and auto-expire
    /// </summary>
    Task<List<DqException>> ExpireExceptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get pending exceptions
    /// </summary>
    Task<List<DqException>> GetPendingExceptionsAsync(CancellationToken ct = default);
}


