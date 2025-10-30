using System.Security.Claims;

namespace IOC.Security;

public interface ITenantContextAccessor
{
    string? TenantId { get; }
    string? SiteId { get; }
    string? AssetId { get; }
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly IHttpContextAccessor _http;

    public TenantContextAccessor(IHttpContextAccessor http) => _http = http;

    public string? TenantId => Resolve(ClaimsSchema.TenantId, "x-tenant-id");
    public string? SiteId => Resolve(ClaimsSchema.SiteId, "x-site-id");
    public string? AssetId => Resolve(ClaimsSchema.AssetId, "x-asset-id");

    private string? Resolve(string claimType, string header)
    {
        var user = _http.HttpContext?.User;
        var claim = user?.FindFirst(claimType)?.Value;
        if (!string.IsNullOrWhiteSpace(claim)) return claim;
        if (_http.HttpContext?.Request.Headers.TryGetValue(header, out var values) == true)
        {
            return values.FirstOrDefault();
        }
        return null;
    }
}

public static class TenantContextExtensions
{
    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        return services;
    }
}
