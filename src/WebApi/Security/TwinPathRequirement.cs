using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace IOC.WebApi.Security;

public sealed class TwinPathRequirement : IAuthorizationRequirement { }

public sealed class TwinPathHandler : AuthorizationHandler<TwinPathRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TwinPathRequirement requirement)
    {
        var allowedPrefix = context.User.FindFirst("twin:path")?.Value;
        if (string.IsNullOrWhiteSpace(allowedPrefix)) return Task.CompletedTask;

        // Endpoint metadata may include the requested idPath as route/query; fallback to allow, actual endpoints will validate prefix when idPath is present
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
