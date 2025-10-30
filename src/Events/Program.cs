using IOC.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddSingleton<EventStore>();
var policy = new SuppressionPolicy { DedupWindow = TimeSpan.FromSeconds(10), ChatterWindow = TimeSpan.FromSeconds(30), FloodThreshold = 100, MaintenanceMode = false };
builder.Services.AddSingleton(policy);
builder.Services.AddSingleton<INotificationSink>(_ => new WebhookNotificationSink("http://localhost:9000/webhook"));
builder.Services.AddSingleton<Router>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/events/ingest", async (string tenant, string site, string asset, RawAlarm raw, Severity sev, Consequence cons, string category, int priority, bool shelved, Router router, CancellationToken ct) =>
{
    var e = await router.RouteAsync(tenant, site, asset, raw, sev, cons, category, priority, shelved, ct);
    return e is null ? Results.Accepted() : Results.Ok(e);
});

app.MapPost("/events/{id:guid}/ack", (Guid id, EventStore store) =>
{
    if (store.TryGet(id, out var e)) { store.Ack(id, DateTimeOffset.UtcNow); return Results.NoContent(); }
    return Results.NotFound();
});

app.MapGet("/events/timeline", (EventStore store) => Results.Ok(store.All()));

app.MapPost("/events/replay", async (string tenant, string site, string asset, List<RawAlarm> alarms, Router router, CancellationToken ct) =>
{
    foreach (var a in alarms)
    {
        await router.RouteAsync(tenant, site, asset, a, Severity.Medium, Consequence.Production, "Replay", 3, false, ct);
    }
    return Results.Accepted();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program {}
