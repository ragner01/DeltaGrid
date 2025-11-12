using IOC.DisasterRecovery.Models;
using IOC.DisasterRecovery.Services;
using IOC.DisasterRecovery.Persistence;
using IOC.DisasterRecovery.Replay;
using IOC.DisasterRecovery.Dashboard;
using IOC.Security;
using IOC.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Configure security features
builder.Services.AddRequestSizeLimits();
builder.Services.AddSecureJwtAuthentication(builder.Configuration);
builder.Services.AddSecureCors(builder.Configuration);

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("TenantScoped", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(ctx => ctx.User.HasClaim(c => c.Type == ClaimsSchema.TenantId));
    });
    opt.AddPolicy("Admin", policy =>
    {
        policy.RequireRole(Roles.Admin);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

// DR services
var repository = new InMemoryDrRepository();
repository.SeedClassifications();

builder.Services.AddSingleton<IBackupRepository>(repository);
builder.Services.AddSingleton<IFailoverRepository>(repository);
builder.Services.AddSingleton<IDrDrillRepository>(repository);
builder.Services.AddSingleton<IDrClassificationService>(repository);
builder.Services.AddSingleton<IEventReplayRepository>(repository);

builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<IFailoverService, FailoverService>();
builder.Services.AddSingleton<IDrDrillService, DrDrillService>();
builder.Services.AddSingleton<IEventReplayService, EventReplayService>();
builder.Services.AddSingleton<IDrDashboard, DrDashboard>();

var app = builder.Build();

// Add security middleware
app.UseSecurityHeaders();
app.UseRequestSizeLimit();
app.UseSerilogRequestLogging();
app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();

// Only enable Swagger in development and if explicitly enabled
if (app.Environment.IsDevelopment() && 
    builder.Configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Disaster Recovery API");
        options.RoutePrefix = string.Empty;
    });
}

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

// DR Classifications
v1.MapGet("/dr/classifications", async (IDrClassificationService service, CancellationToken ct) =>
{
    var classifications = await service.GetAllClassificationsAsync(ct);
    return Results.Ok(classifications);
});

v1.MapPost("/dr/classifications", async (ServiceDrClassification classification, IDrClassificationService service, CancellationToken ct) =>
{
    await service.SaveClassificationAsync(classification, ct);
    return Results.Created($"/api/v1/dr/classifications/{classification.ServiceId}", classification);
}).RequireAuthorization("Admin");

// DR Readiness Status
v1.MapGet("/dr/readiness", async (IDrDrillService service, CancellationToken ct) =>
{
    var statuses = await service.GetReadinessStatusAsync(ct);
    return Results.Ok(statuses);
});

// Backups
v1.MapPost("/dr/backups/{backupId}/execute", async (string backupId, IBackupService service, CancellationToken ct) =>
{
    var execution = await service.ExecuteBackupAsync(backupId, ct);
    return Results.Ok(execution);
}).RequireAuthorization("Admin");

v1.MapGet("/dr/backups/{backupId}/history", async (string backupId, IBackupService service, CancellationToken ct) =>
{
    var history = await service.GetBackupHistoryAsync(backupId, 100, ct);
    return Results.Ok(history);
});

v1.MapPost("/dr/backups/{backupId}/validate", async (string backupId, IBackupService service, CancellationToken ct) =>
{
    var isValid = await service.ValidateBackupAsync(backupId, ct);
    return Results.Ok(new { valid = isValid });
});

v1.MapPost("/dr/backups/{backupId}/restore-test", async (string backupId, IBackupService service, CancellationToken ct) =>
{
    var test = await service.RunRestoreTestAsync(backupId, ct);
    return Results.Ok(test);
}).RequireAuthorization("Admin");

// Failover
v1.MapPost("/dr/failover/{failoverId}/execute", async (string failoverId, IFailoverService service, CancellationToken ct) =>
{
    await service.ExecuteFailoverAsync(failoverId, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapPost("/dr/failover/{failoverId}/test", async (string failoverId, IFailoverService service, CancellationToken ct) =>
{
    await service.TestFailoverAsync(failoverId, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapGet("/dr/failover/{serviceId}/status", async (string serviceId, IFailoverService service, CancellationToken ct) =>
{
    var status = await service.GetFailoverStatusAsync(serviceId, ct);
    return Results.Ok(status);
});

// DR Drills
v1.MapPost("/dr/drills", async (DrDrill drill, IDrDrillService service, CancellationToken ct) =>
{
    var scheduled = await service.ScheduleDrillAsync(drill, ct);
    return Results.Created($"/api/v1/dr/drills/{scheduled.Id}", scheduled);
}).RequireAuthorization("Admin");

v1.MapPost("/dr/drills/{drillId}/execute", async (string drillId, IDrDrillService service, CancellationToken ct) =>
{
    var drill = await service.ExecuteDrillAsync(drillId, ct);
    return Results.Ok(drill);
}).RequireAuthorization("Admin");

v1.MapGet("/dr/drills/{drillId}", async (string drillId, IDrDrillService service, CancellationToken ct) =>
{
    var drill = await service.GetDrillAsync(drillId, ct);
    return drill != null ? Results.Ok(drill) : Results.NotFound();
});

// Event Replay
v1.MapPost("/dr/replay", async (ReplayRequest request, IEventReplayService service, CancellationToken ct) =>
{
    var result = await service.ReplayEventsAsync(request, ct);
    return Results.Ok(result);
}).RequireAuthorization("Admin");

v1.MapPost("/dr/replay/ingestion", async (ReplayRequest request, IEventReplayService service, CancellationToken ct) =>
{
    var result = await service.ReplayIngestionEventsAsync(request, ct);
    return Results.Ok(result);
}).RequireAuthorization("Admin");

v1.MapGet("/dr/replay/history", async (IEventReplayService service, CancellationToken ct) =>
{
    var history = await service.GetReplayHistoryAsync(ct);
    return Results.Ok(history);
});

// DR Dashboard
v1.MapGet("/dr/dashboard", async (IDrDashboard dashboard, CancellationToken ct) =>
{
    var data = await dashboard.GetDashboardAsync(ct);
    return Results.Ok(data);
});

v1.MapGet("/dr/metrics", async (IDrDashboard dashboard, CancellationToken ct) =>
{
    var metrics = await dashboard.GetMetricsAsync(ct);
    return Results.Ok(metrics);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "disaster-recovery" }));

app.Run();

public partial class Program { }

