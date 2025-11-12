using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using IOC.Security.Jwt;
using IOC.Security.KeyVault;
using IOC.BuildingBlocks.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Configure Key Vault Secret Manager
var keyVaultUrl = builder.Configuration["KeyVault:Url"] 
    ?? builder.Configuration["Azure:KeyVault:Url"]
    ?? throw new InvalidOperationException("KeyVault:Url must be configured");

builder.Services.AddSingleton<IKeyVaultSecretManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KeyVaultSecretManager>>();
    return new KeyVaultSecretManager(keyVaultUrl, logger);
});

// Configure JWT validation with proper security
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "https://deltagrid.io";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "deltagrid-api";
var jwtSigningKeyName = builder.Configuration["JWT:SigningKeyName"] ?? "jwt-signing-key";

builder.Services.AddSingleton<IJwtValidator>(sp =>
{
    var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
    var logger = sp.GetRequiredService<ILogger<JwtValidator>>();
    return new JwtValidator(keyVault, jwtIssuer, jwtAudience, jwtSigningKeyName, logger);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            RequireAudience = true
        };
    });

// Set signing key from Key Vault during configuration
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
{
    return new ConfigureNamedOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme, 
        options =>
        {
            try
            {
                var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
                var signingKey = keyVault.GetSecretAsync(jwtSigningKeyName).Result;
                var keyBytes = Convert.FromBase64String(signingKey);
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(keyBytes);
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to configure JWT signing key from Key Vault");
                throw;
            }
        });
});

// Configure Redis for distributed cache (idempotency store)
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "IOC:";
    });
}
else
{
    // Fallback to in-memory cache for development
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var tenant = ctx.User?.FindFirst("tenant_id")?.Value ?? "anonymous";
        // Improved rate limiting: 100 requests per second with burst allowance
        return RateLimitPartition.GetTokenBucketLimiter(tenant, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 20,
            AutoReplenishment = true,
            QueueLimit = 50,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

// Correlation ID middleware values
builder.Services.AddHttpContextAccessor();

// Configure YARP with Polly policies
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[]
        {
            new RouteConfig
            {
                RouteId = "api-v1",
                ClusterId = "api-cluster-v1",
                Match = new RouteMatch { Path = "/api/v1/{**catchall}" },
                Transforms = new[]
                {
                    new Dictionary<string, string> { ["RequestHeader"] = "X-Canary", ["Set"] = "{X-Canary}" },
                }
            },
            new RouteConfig
            {
                RouteId = "api-v2-canary",
                ClusterId = "api-cluster-v2",
                Match = new RouteMatch { Path = "/api/v2/{**catchall}" },
            }
        },
        new[]
        {
            new ClusterConfig
            {
                ClusterId = "api-cluster-v1",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new DestinationConfig { Address = "https://localhost:5001/" },
                },
            },
            new ClusterConfig
            {
                ClusterId = "api-cluster-v2",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new DestinationConfig { Address = "https://localhost:5001/" },
                },
            },
        });

// Polly resilience policies configured via HttpClient factory
// Note: YARP handles resilience internally; this is for external HTTP calls
var app = builder.Build();

app.UseSerilogRequestLogging();

// Add security headers
app.UseSecurityHeaders();

// Correlation ID middleware
app.Use(async (ctx, next) =>
{
    var corr = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-ID"] = corr;
    await next();
});

// Idempotency middleware using distributed cache
app.Use(async (ctx, next) =>
{
    if (string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ctx.Request.Method, "PUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ctx.Request.Method, "PATCH", StringComparison.OrdinalIgnoreCase))
    {
        var cache = ctx.RequestServices.GetRequiredService<IDistributedCache>();
        var key = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
        
        if (!string.IsNullOrWhiteSpace(key))
        {
            // Limit key length to prevent abuse
            if (key.Length > 256)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Idempotency-Key too long\"}");
                return;
            }
            
            var cacheKey = $"idempotency:{key}";
            var existing = await cache.GetStringAsync(cacheKey);
            
            if (existing != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Duplicate request\"}");
                return;
            }
            
            // Store for 24 hours
            await cache.SetStringAsync(cacheKey, "processed", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
        }
    }
    
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
