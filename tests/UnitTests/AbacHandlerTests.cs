using System.Security.Claims;
using FluentAssertions;
using IOC.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;
using System.Threading.Tasks;

namespace IOC.UnitTests;

public class AbacHandlerTests
{
    [Fact]
    public async Task Succeeds_When_Tenant_Matches()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimsSchema.TenantId, "tenant-a")
        }, "test"));

        var requirement = new AbacRequirement(tenantId: "tenant-a");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = new AbacHandler();
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_When_Tenant_Differs()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimsSchema.TenantId, "tenant-b")
        }, "test"));

        var requirement = new AbacRequirement(tenantId: "tenant-a");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        var handler = new AbacHandler();
        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
