using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddDbContext<AuthDb>(opt => opt.UseInMemoryDatabase("auth"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDb>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer()
    .AddInMemoryIdentityResources(new[]
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("tenant", new[]{"tenant_id","site_id","asset_id","discipline","shift","permit_level"})
    })
    .AddInMemoryApiScopes(new[]
    {
        new ApiScope("api", "IOC API")
    })
    .AddInMemoryClients(new[]
    {
        // Ops Console Web (auth code + PKCE)
        new Client
        {
            ClientId = "ops-console",
            ClientName = "Ops Console",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            RedirectUris = { "https://localhost:5001/signin-oidc" },
            PostLogoutRedirectUris = { "https://localhost:5001/signout-callback-oidc" },
            AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile, "tenant", "api" },
            AllowOfflineAccess = true
        },
        // Field device (device code)
        new Client
        {
            ClientId = "field-device",
            ClientName = "Field Device",
            AllowedGrantTypes = GrantTypes.DeviceFlow,
            RequireClientSecret = false,
            AllowedScopes = { IdentityServerConstants.StandardScopes.OpenId, IdentityServerConstants.StandardScopes.Profile, "tenant", "api" },
            AllowOfflineAccess = true
        },
        // Service to service (client credentials)
        new Client
        {
            ClientId = "svc-gateway",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" }
        }
    })
    .AddAspNetIdentity<IdentityUser>();

builder.Services.AddAuthentication();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseIdentityServer();

await SeedAsync(app.Services);

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

static async Task SeedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = new[]{"ControlRoomOperator","ProductionEngineer","MaintenancePlanner","IntegrityEngineer","HSELead","Auditor","Admin"};
    foreach (var r in roles)
    {
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));
    }

    async Task CreateUser(string username, string role, params (string,string)[] claims)
    {
        var u = await userMgr.FindByNameAsync(username);
        if (u is null)
        {
            u = new IdentityUser(username) { Email = $"{username}@example.com", EmailConfirmed = true };
            await userMgr.CreateAsync(u, "Pass123$!");
            await userMgr.AddToRoleAsync(u, role);
            foreach (var (type, value) in claims)
            {
                await userMgr.AddClaimAsync(u, new System.Security.Claims.Claim(type, value));
            }
        }
    }

    await CreateUser("operator1", "ControlRoomOperator",
        ("tenant_id","tenant-a"),("site_id","site-1"),("discipline","operations"),("shift","day"),("permit_level","2"));

    await CreateUser("engineer1", "ProductionEngineer",
        ("tenant_id","tenant-a"),("site_id","site-1"),("discipline","production"),("permit_level","3"));
}

class AuthDb : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AuthDb(DbContextOptions<AuthDb> options) : base(options) {}
}
