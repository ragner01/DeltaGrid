using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace IOC.Security;

public sealed class AbacRequirement : IAuthorizationRequirement
{
    public string? RequiredTenantId { get; }
    public string? RequiredSiteId { get; }
    public string? RequiredAssetId { get; }

    public AbacRequirement(string? tenantId = null, string? siteId = null, string? assetId = null)
    {
        RequiredTenantId = tenantId;
        RequiredSiteId = siteId;
        RequiredAssetId = assetId;
    }
}

public sealed class AbacHandler : AuthorizationHandler<AbacRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AbacRequirement requirement)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        bool ok = true;

        if (requirement.RequiredTenantId is not null)
        {
            ok &= HasClaim(user, ClaimsSchema.TenantId, requirement.RequiredTenantId);
        }
        if (requirement.RequiredSiteId is not null)
        {
            ok &= HasClaim(user, ClaimsSchema.SiteId, requirement.RequiredSiteId);
        }
        if (requirement.RequiredAssetId is not null)
        {
            ok &= HasClaim(user, ClaimsSchema.AssetId, requirement.RequiredAssetId);
        }

        if (ok)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasClaim(ClaimsPrincipal user, string type, string value)
    {
        return user.FindAll(type).Any(c => string.Equals(c.Value, value, StringComparison.OrdinalIgnoreCase));
    }
}
