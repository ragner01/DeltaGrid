using IOC.Reporting.Models;
using IOC.Reporting.Services;
using IOC.Reporting.Templates;
using IOC.Reporting.Export;
using IOC.Reporting.Persistence;
using IOC.Reporting.Scheduling;
using IOC.Security;
using IOC.BuildingBlocks.Security;
using Hangfire;
using Hangfire.SqlServer;
using Hangfire.MemoryStorage;
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

// Hangfire for scheduling
builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer();
    config.UseDefaultTypeSerializer();
    config.UseMemoryStorage(); // Use SQL Server in production
});
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
});

// Reporting services
builder.Services.AddSingleton<ITemplateEngine, JsonTemplateEngine>();
builder.Services.AddSingleton<IPdfExporter, QuestPdfExporter>();
builder.Services.AddSingleton<IExcelExporter, ClosedXmlExporter>();
builder.Services.AddSingleton<ICsvExporter, CsvExporter>();

var repo = new InMemoryReportRepository();
repo.SeedTemplates();
builder.Services.AddSingleton<IReportRepository>(repo);
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IReportScheduler, HangfireReportScheduler>();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Reporting API");
        options.RoutePrefix = string.Empty;
    });
    app.UseHangfireDashboard("/hangfire");
}

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

// Generate report
v1.MapPost("/reports/generate", async (ReportRequest request, IReportService reportService, HttpContext http, CancellationToken ct) =>
{
    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    if (string.IsNullOrEmpty(tenantId) || request.TenantId != tenantId)
    {
        return Results.Forbid();
    }

    var report = await reportService.GenerateAsync(request, ct);
    return Results.Ok(new { reportId = report.Id, fileName = report.FileName, status = report.Status.ToString() });
});

// Get report
v1.MapGet("/reports/{reportId}", async (string reportId, IReportRepository repo, HttpContext http, CancellationToken ct) =>
{
    var report = await repo.GetReportAsync(reportId, ct);
    if (report == null)
    {
        return Results.NotFound();
    }

    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    if (report.TenantId != tenantId)
    {
        return Results.Forbid();
    }

    // Log access
    var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var userName = http.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown";
    await repo.LogAccessAsync(reportId, new ReportAccessLog
    {
        UserId = userId,
        UserName = userName,
        AccessedAt = DateTimeOffset.UtcNow,
        Action = "VIEWED",
        IpAddress = http.Connection.RemoteIpAddress?.ToString(),
        UserAgent = http.Request.Headers["User-Agent"].ToString()
    }, ct);

    return Results.File(report.Content, report.ContentType, report.FileName);
});

// Sign report
v1.MapPost("/reports/{reportId}/sign", async (string reportId, SignReportRequest body, IReportService reportService, HttpContext http, CancellationToken ct) =>
{
    var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var userName = http.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown";
    var role = http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown";

    var signature = await reportService.SignAsync(reportId, userId, userName, role, body.Comment, ct);
    return Results.Ok(new { signatureId = signature.SignatureHash, signedAt = signature.SignedAt });
});

// Archive report
v1.MapPost("/reports/{reportId}/archive", async (string reportId, IReportService reportService, CancellationToken ct) =>
{
    await reportService.ArchiveAsync(reportId, ct);
    return Results.NoContent();
});

// Get archive
v1.MapGet("/reports/{reportId}/archive", async (string reportId, IReportService reportService, HttpContext http, CancellationToken ct) =>
{
    var archive = await reportService.GetArchiveAsync(reportId, ct);
    if (archive == null)
    {
        return Results.NotFound();
    }

    // Log access
    var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    var userName = http.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown";
    await reportService.LogAccessAsync(reportId, userId, userName, "VIEWED", http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers["User-Agent"].ToString(), ct);

    return Results.File(archive.Content, archive.ContentType, archive.FileName);
});

// Schedule report
v1.MapPost("/reports/schedule", async (ScheduledReport schedule, IReportScheduler scheduler, HttpContext http, CancellationToken ct) =>
{
    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    if (string.IsNullOrEmpty(tenantId) || schedule.TenantId != tenantId)
    {
        return Results.Forbid();
    }

    var scheduleId = await scheduler.ScheduleAsync(schedule, ct);
    return Results.Created($"/api/v1/reports/schedules/{scheduleId}", new { scheduleId });
});

// Get schedule
v1.MapGet("/reports/schedules/{scheduleId}", async (string scheduleId, IReportRepository repo, HttpContext http, CancellationToken ct) =>
{
    var schedule = await repo.GetScheduleAsync(scheduleId, ct);
    if (schedule == null)
    {
        return Results.NotFound();
    }

    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    if (schedule.TenantId != tenantId)
    {
        return Results.Forbid();
    }

    return Results.Ok(schedule);
});

// Trigger schedule now
v1.MapPost("/reports/schedules/{scheduleId}/trigger", async (string scheduleId, IReportScheduler scheduler, CancellationToken ct) =>
{
    await scheduler.TriggerNowAsync(scheduleId, ct);
    return Results.Accepted();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "reporting" }));

app.Run();

public sealed record SignReportRequest(string? Comment);

public partial class Program { }
