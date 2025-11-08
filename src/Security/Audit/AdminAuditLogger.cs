using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IOC.Security.Audit;

/// <summary>
/// Admin action audit logger for compliance and security
/// </summary>
public interface IAdminAuditLogger
{
    /// <summary>
    /// Log an admin action
    /// </summary>
    Task LogActionAsync(AdminAction action, CancellationToken ct = default);

    /// <summary>
    /// Query admin action logs
    /// </summary>
    Task<List<AdminAction>> QueryLogsAsync(AdminAuditQuery query, CancellationToken ct = default);
}

/// <summary>
/// Admin action record
/// </summary>
public sealed class AdminAction
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string Action { get; init; } // e.g., "CREATE_USER", "DELETE_PERMIT", "ROTATE_KEY"
    public required string ResourceType { get; init; } // e.g., "User", "Permit", "Key"
    public string? ResourceId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Admin audit query
/// </summary>
public sealed class AdminAuditQuery
{
    public string? UserId { get; init; }
    public string? Action { get; init; }
    public string? ResourceType { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 100;
}

/// <summary>
/// In-memory admin audit logger (replace with persistent store in production)
/// </summary>
public sealed class InMemoryAdminAuditLogger : IAdminAuditLogger
{
    private readonly List<AdminAction> _actions = new();
    private readonly ILogger<InMemoryAdminAuditLogger> _logger;

    public InMemoryAdminAuditLogger(ILogger<InMemoryAdminAuditLogger> logger)
    {
        _logger = logger;
    }

    public Task LogActionAsync(AdminAction action, CancellationToken ct = default)
    {
        _actions.Add(action);
        _logger.LogInformation("Admin action logged: {UserId} {Action} {ResourceType} {ResourceId}",
            action.UserId, action.Action, action.ResourceType, action.ResourceId);
        return Task.CompletedTask;
    }

    public Task<List<AdminAction>> QueryLogsAsync(AdminAuditQuery query, CancellationToken ct = default)
    {
        var results = _actions.AsEnumerable();

        if (!string.IsNullOrEmpty(query.UserId))
        {
            results = results.Where(a => a.UserId == query.UserId);
        }

        if (!string.IsNullOrEmpty(query.Action))
        {
            results = results.Where(a => a.Action == query.Action);
        }

        if (!string.IsNullOrEmpty(query.ResourceType))
        {
            results = results.Where(a => a.ResourceType == query.ResourceType);
        }

        if (query.FromDate.HasValue)
        {
            results = results.Where(a => a.Timestamp >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            results = results.Where(a => a.Timestamp <= query.ToDate.Value);
        }

        return Task.FromResult(results
            .OrderByDescending(a => a.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToList());
    }
}

/// <summary>
/// Extension methods for admin audit logging
/// </summary>
public static class AdminAuditExtensions
{
    public static async Task LogAdminActionAsync(
        this IAdminAuditLogger logger,
        ClaimsPrincipal user,
        string action,
        string resourceType,
        string? resourceId = null,
        HttpContext? httpContext = null,
        bool success = true,
        string? errorMessage = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userName = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown";

        var adminAction = new AdminAction
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Timestamp = DateTimeOffset.UtcNow,
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
            Metadata = metadata ?? new Dictionary<string, string>(),
            Success = success,
            ErrorMessage = errorMessage
        };

        await logger.LogActionAsync(adminAction, ct);
    }
}

