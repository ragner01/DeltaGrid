using Microsoft.AspNetCore.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var tenant = ctx.User?.FindFirst("tenant_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(tenant, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromSeconds(1),
            QueueLimit = 0,
            AutoReplenishment = true
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
                    new Dictionary<string,string> { ["RequestHeader"] = "X-Canary", ["Set"] = "{X-Canary}" }
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
                    ["d1"] = new DestinationConfig{ Address = "https://localhost:5001/" }
                }
            },
            new ClusterConfig
            {
                ClusterId = "api-cluster-v2",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new DestinationConfig{ Address = "https://localhost:5001/" }
                }
            }
        }
    );

builder.Services.AddSingleton<HttpMessageHandlerBuilderFilter, PollyHttpMessageHandlerBuilderFilter>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    await next();
});

// Correlation ID + Idempotency middleware
app.Use(async (ctx, next) =>
{
    var corr = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-ID"] = corr;
    await next();
});

var idempotencyStore = new Dictionary<string, DateTimeOffset>();
app.Use(async (ctx, next) =>
{
    if (string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
    {
        var key = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (idempotencyStore.ContainsKey(key)) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsync("duplicate"); return; }
            idempotencyStore[key] = DateTimeOffset.UtcNow;
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

public partial class Program {}

public sealed class PollyHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            var retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1) });
            var breaker = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(retry));
            builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(breaker));
            builder.AdditionalHandlers.Add(new TimeoutHandler(TimeSpan.FromSeconds(15)));
        };
    }
}

public sealed class TimeoutHandler : DelegatingHandler
{
    private readonly TimeSpan _timeout;
    public TimeoutHandler(TimeSpan timeout) { _timeout = timeout; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        return await base.SendAsync(request, cts.Token);
    }
}
