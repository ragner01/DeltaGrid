using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

/// <summary>
/// DQ exception service implementation
/// </summary>
public sealed class DqExceptionService : IDqExceptionService
{
    private readonly IDqExceptionRepository _exceptionRepository;
    private readonly IDqBreachRepository _breachRepository;
    private readonly ILogger<DqExceptionService> _logger;

    public DqExceptionService(
        IDqExceptionRepository exceptionRepository,
        IDqBreachRepository breachRepository,
        ILogger<DqExceptionService> logger)
    {
        _exceptionRepository = exceptionRepository;
        _breachRepository = breachRepository;
        _logger = logger;
    }

    public async Task<DqException> RequestExceptionAsync(DqException exception, CancellationToken ct = default)
    {
        await _exceptionRepository.SaveExceptionAsync(exception, ct);
        _logger.LogInformation("DQ exception requested: {ExceptionId} for breach {BreachId} by {User}",
            exception.Id, exception.BreachId, exception.RequestedBy);
        return exception;
    }

    public async Task ApproveExceptionAsync(string exceptionId, string approvedBy, CancellationToken ct = default)
    {
        var exception = await _exceptionRepository.GetExceptionAsync(exceptionId, ct);
        if (exception == null)
        {
            throw new InvalidOperationException($"Exception {exceptionId} not found");
        }

        if (exception.Status != DqExceptionStatus.Pending)
        {
            throw new InvalidOperationException($"Exception {exceptionId} is not pending");
        }

        exception = exception with
        {
            Status = DqExceptionStatus.Approved,
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedBy = approvedBy
        };

        await _exceptionRepository.SaveExceptionAsync(exception, ct);

        // Update breach status
        var breach = await _breachRepository.GetBreachAsync(exception.BreachId, ct);
        if (breach != null)
        {
            breach = breach with
            {
                Status = DqBreachStatus.Exception,
                ExceptionId = exception.Id
            };

            await _breachRepository.SaveBreachAsync(breach, ct);
        }

        _logger.LogInformation("DQ exception {ExceptionId} approved by {User}", exceptionId, approvedBy);
    }

    public async Task RejectExceptionAsync(string exceptionId, string rejectedBy, string reason, CancellationToken ct = default)
    {
        var exception = await _exceptionRepository.GetExceptionAsync(exceptionId, ct);
        if (exception == null)
        {
            throw new InvalidOperationException($"Exception {exceptionId} not found");
        }

        if (exception.Status != DqExceptionStatus.Pending)
        {
            throw new InvalidOperationException($"Exception {exceptionId} is not pending");
        }

        exception = exception with
        {
            Status = DqExceptionStatus.Rejected,
            RejectionReason = reason
        };

        await _exceptionRepository.SaveExceptionAsync(exception, ct);
        _logger.LogInformation("DQ exception {ExceptionId} rejected by {User}: {Reason}", exceptionId, rejectedBy, reason);
    }

    public async Task<List<DqException>> ExpireExceptionsAsync(CancellationToken ct = default)
    {
        var exceptions = await _exceptionRepository.GetApprovedExceptionsWithExpiryAsync(ct);
        var expired = new List<DqException>();

        foreach (var exception in exceptions)
        {
            if (exception.ExpiresAt.HasValue && exception.ExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                var updatedException = exception with
                {
                    Status = DqExceptionStatus.Expired
                };

                await _exceptionRepository.SaveExceptionAsync(updatedException, ct);
                expired.Add(updatedException);

                // Reopen breach when exception expires
                var breach = await _breachRepository.GetBreachAsync(exception.BreachId, ct);
                if (breach != null)
                {
                    breach = breach with
                    {
                        Status = DqBreachStatus.Open,
                        ExceptionId = null
                    };

                    await _breachRepository.SaveBreachAsync(breach, ct);
                }

                _logger.LogInformation("DQ exception {ExceptionId} expired automatically", exception.Id);
            }
        }

        return expired;
    }

    public Task<List<DqException>> GetPendingExceptionsAsync(CancellationToken ct = default)
    {
        return _exceptionRepository.GetPendingExceptionsAsync(ct);
    }
}

/// <summary>
/// DQ exception repository interface
/// </summary>
public interface IDqExceptionRepository
{
    Task<DqException?> GetExceptionAsync(string exceptionId, CancellationToken ct = default);
    Task<List<DqException>> GetPendingExceptionsAsync(CancellationToken ct = default);
    Task<List<DqException>> GetApprovedExceptionsWithExpiryAsync(CancellationToken ct = default);
    Task SaveExceptionAsync(DqException exception, CancellationToken ct = default);
}


