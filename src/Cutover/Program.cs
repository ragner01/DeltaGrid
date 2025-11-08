using IOC.Cutover.Models;
using IOC.Cutover.Services;
using IOC.Cutover.Persistence;
using IOC.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = true
        };
    });

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

// Cutover services
var repository = new InMemoryCutoverRepository();
repository.SeedDefaultData();

builder.Services.AddSingleton<ISeedDataRepository>(repository);
builder.Services.AddSingleton<ICutoverRepository>(repository);
builder.Services.AddSingleton<IFeatureFlagRepository>(repository);
builder.Services.AddSingleton<IHypercareRepository>(repository);

builder.Services.AddSingleton<ISeedDataService, SeedDataService>();
builder.Services.AddSingleton<ICutoverService, CutoverService>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddSingleton<IHypercareService, HypercareService>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

// Seed Data
v1.MapPost("/cutover/seed/all", async (ISeedDataService service, string createdBy, CancellationToken ct) =>
{
    var result = await service.SeedAllAsync(createdBy, ct);
    return Results.Ok(result);
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/seed/{type}", async (SeedDataType type, ISeedDataService service, string createdBy, CancellationToken ct) =>
{
    var result = await service.SeedAsync(type, createdBy, ct);
    return Results.Ok(result);
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/seed/validate", async (ISeedDataService service, CancellationToken ct) =>
{
    var result = await service.ValidateAsync(ct);
    return Results.Ok(result);
});

v1.MapPost("/cutover/seed/clear", async (ISeedDataService service, CancellationToken ct) =>
{
    await service.ClearAsync(ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

// Cutover Management
v1.MapPost("/cutover", async (CutoverExecution cutover, ICutoverService service, CancellationToken ct) =>
{
    var created = await service.CreateCutoverAsync(cutover, ct);
    return Results.Created($"/api/v1/cutover/{created.Id}", created);
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/{cutoverId}/start", async (string cutoverId, ICutoverService service, CancellationToken ct) =>
{
    await service.StartCutoverAsync(cutoverId, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/{cutoverId}/phase/{phase}", async (string cutoverId, CutoverPhase phase, ICutoverService service, CancellationToken ct) =>
{
    await service.CompletePhaseAsync(cutoverId, phase, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapGet("/cutover/{cutoverId}", async (string cutoverId, ICutoverService service, CancellationToken ct) =>
{
    var cutover = await service.GetCutoverAsync(cutoverId, ct);
    return cutover != null ? Results.Ok(cutover) : Results.NotFound();
});

v1.MapGet("/cutover/readiness", async (ICutoverService service, CancellationToken ct) =>
{
    var readiness = await service.GetReadinessStatusAsync(ct);
    return Results.Ok(readiness);
});

v1.MapPost("/cutover/{cutoverId}/rollback", async (string cutoverId, ICutoverService service, string executedBy, CancellationToken ct) =>
{
    await service.ExecuteRollbackAsync(cutoverId, executedBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

// Feature Flags
v1.MapPost("/cutover/flags", async (FeatureFlag flag, IFeatureFlagService service, CancellationToken ct) =>
{
    var created = await service.CreateFlagAsync(flag, ct);
    return Results.Created($"/api/v1/cutover/flags/{created.Id}", created);
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/flags/{flagId}/enable", async (string flagId, IFeatureFlagService service, string enabledBy, CancellationToken ct) =>
{
    await service.EnableFlagAsync(flagId, enabledBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapPost("/cutover/flags/{flagId}/disable", async (string flagId, IFeatureFlagService service, string disabledBy, CancellationToken ct) =>
{
    await service.DisableFlagAsync(flagId, disabledBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapGet("/cutover/flags/{flagId}", async (string flagId, IFeatureFlagService service, CancellationToken ct) =>
{
    var flag = await service.GetFlagAsync(flagId, ct);
    return flag != null ? Results.Ok(flag) : Results.NotFound();
});

v1.MapGet("/cutover/flags/{flagId}/enabled", async (string flagId, IFeatureFlagService service, string? tenantId, string? userId, CancellationToken ct) =>
{
    var enabled = await service.IsEnabledAsync(flagId, tenantId, userId, ct);
    return Results.Ok(new { enabled });
});

// Hypercare
v1.MapPost("/cutover/incidents", async (HypercareIncident incident, IHypercareService service, CancellationToken ct) =>
{
    var created = await service.ReportIncidentAsync(incident, ct);
    return Results.Created($"/api/v1/cutover/incidents/{created.Id}", created);
});

v1.MapPost("/cutover/incidents/{incidentId}/assign", async (string incidentId, IHypercareService service, string assignedTo, CancellationToken ct) =>
{
    await service.AssignIncidentAsync(incidentId, assignedTo, ct);
    return Results.NoContent();
});

v1.MapPost("/cutover/incidents/{incidentId}/resolve", async (string incidentId, IHypercareService service, string resolvedBy, string resolution, CancellationToken ct) =>
{
    await service.ResolveIncidentAsync(incidentId, resolvedBy, resolution, ct);
    return Results.NoContent();
});

v1.MapGet("/cutover/incidents/open", async (IHypercareService service, string? cutoverId, IncidentSeverity? severity, CancellationToken ct) =>
{
    var incidents = await service.GetOpenIncidentsAsync(cutoverId, severity, ct);
    return Results.Ok(incidents);
});

v1.MapGet("/cutover/{cutoverId}/statistics", async (string cutoverId, IHypercareService service, CancellationToken ct) =>
{
    var statistics = await service.GetStatisticsAsync(cutoverId, ct);
    return Results.Ok(statistics);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "cutover" }));

app.Run();

public partial class Program { }


