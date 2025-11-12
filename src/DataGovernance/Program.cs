using IOC.DataGovernance.Models;
using IOC.DataGovernance.Services;
using IOC.DataGovernance.Persistence;
using IOC.DataGovernance.Dashboard;
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
    opt.AddPolicy("Steward", policy =>
    {
        policy.RequireRole(Roles.ProductionEngineer, Roles.DataSteward);
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

// Governance services
var repository = new InMemoryGovernanceRepository();
repository.SeedData();

builder.Services.AddSingleton<IDqRuleRepository>(repository);
builder.Services.AddSingleton<IDqScoreRepository>(repository);
builder.Services.AddSingleton<IDqBreachRepository>(repository);
builder.Services.AddSingleton<IDqExceptionRepository>(repository);
builder.Services.AddSingleton<IAccessRequestRepository>(repository);
builder.Services.AddSingleton<ILineageRepository>(repository);
builder.Services.AddSingleton<IDatasetMetadataRepository>(repository);

builder.Services.AddSingleton<IDqEngine, DqEngine>();
builder.Services.AddSingleton<IDqBreachService, DqBreachService>();
builder.Services.AddSingleton<IDqExceptionService, DqExceptionService>();
builder.Services.AddSingleton<IAccessRequestService, AccessRequestService>();
builder.Services.AddSingleton<ILineageService, LineageService>();
builder.Services.AddSingleton<IStewardDashboard, StewardDashboard>();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Data Governance API");
        options.RoutePrefix = string.Empty;
    });
}

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

// Data Quality Rules
v1.MapGet("/dq/rules", async (IDqRuleRepository repository, CancellationToken ct) =>
{
    var rules = await repository.GetAllActiveRulesAsync(ct);
    return Results.Ok(rules);
});

v1.MapPost("/dq/rules", async (DqRule rule, IDqRuleRepository repository, CancellationToken ct) =>
{
    await repository.SaveRuleAsync(rule, ct);
    return Results.Created($"/api/v1/dq/rules/{rule.Id}", rule);
}).RequireAuthorization("Steward");

v1.MapGet("/dq/rules/{ruleId}", async (string ruleId, IDqRuleRepository repository, CancellationToken ct) =>
{
    var rule = await repository.GetRuleAsync(ruleId, ct);
    return rule != null ? Results.Ok(rule) : Results.NotFound();
});

// Data Quality Evaluation
v1.MapPost("/dq/evaluate/dataset/{datasetId}", async (string datasetId, IDqEngine engine, CancellationToken ct) =>
{
    var scores = await engine.EvaluateDatasetAsync(datasetId, ct);
    return Results.Ok(scores);
}).RequireAuthorization("Steward");

v1.MapPost("/dq/evaluate/rule/{ruleId}", async (string ruleId, IDqEngine engine, CancellationToken ct) =>
{
    var score = await engine.EvaluateRuleAsync(ruleId, ct);
    return Results.Ok(score);
}).RequireAuthorization("Steward");

v1.MapPost("/dq/evaluate/all", async (IDqEngine engine, CancellationToken ct) =>
{
    var scores = await engine.EvaluateAllRulesAsync(ct);
    return Results.Ok(scores);
}).RequireAuthorization("Steward");

v1.MapGet("/dq/scores/dataset/{datasetId}", async (string datasetId, IDqEngine engine, CancellationToken ct) =>
{
    var scores = await engine.GetDatasetScoreAsync(datasetId, ct);
    return Results.Ok(scores);
});

// Data Quality Breaches
v1.MapPost("/dq/breaches/detect", async (IDqBreachService service, CancellationToken ct) =>
{
    var breaches = await service.DetectBreachesAsync(ct);
    return Results.Ok(breaches);
}).RequireAuthorization("Steward");

v1.MapGet("/dq/breaches/open", async (IDqBreachService service, string? datasetId, CancellationToken ct) =>
{
    var breaches = await service.GetOpenBreachesAsync(datasetId, ct);
    return Results.Ok(breaches);
});

v1.MapPost("/dq/breaches/{breachId}/acknowledge", async (string breachId, IDqBreachService service, string acknowledgedBy, CancellationToken ct) =>
{
    await service.AcknowledgeBreachAsync(breachId, acknowledgedBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Steward");

v1.MapPost("/dq/breaches/{breachId}/resolve", async (string breachId, IDqBreachService service, string resolvedBy, string? notes, CancellationToken ct) =>
{
    await service.ResolveBreachAsync(breachId, resolvedBy, notes, ct);
    return Results.NoContent();
}).RequireAuthorization("Steward");

v1.MapGet("/dq/breaches/statistics", async (IDqBreachService service, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct) =>
{
    var statistics = await service.GetBreachStatisticsAsync(fromDate, toDate, ct);
    return Results.Ok(statistics);
});

// DQ Exceptions
v1.MapPost("/dq/exceptions", async (DqException exception, IDqExceptionService service, CancellationToken ct) =>
{
    var created = await service.RequestExceptionAsync(exception, ct);
    return Results.Created($"/api/v1/dq/exceptions/{created.Id}", created);
}).RequireAuthorization("Steward");

v1.MapPost("/dq/exceptions/{exceptionId}/approve", async (string exceptionId, IDqExceptionService service, string approvedBy, CancellationToken ct) =>
{
    await service.ApproveExceptionAsync(exceptionId, approvedBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapPost("/dq/exceptions/{exceptionId}/reject", async (string exceptionId, IDqExceptionService service, string rejectedBy, string reason, CancellationToken ct) =>
{
    await service.RejectExceptionAsync(exceptionId, rejectedBy, reason, ct);
    return Results.NoContent();
}).RequireAuthorization("Admin");

v1.MapPost("/dq/exceptions/expire", async (IDqExceptionService service, CancellationToken ct) =>
{
    var expired = await service.ExpireExceptionsAsync(ct);
    return Results.Ok(expired);
}).RequireAuthorization("Admin");

// Access Requests
v1.MapPost("/access/requests", async (AccessRequest request, IAccessRequestService service, CancellationToken ct) =>
{
    var created = await service.CreateRequestAsync(request, ct);
    return Results.Created($"/api/v1/access/requests/{created.Id}", created);
});

v1.MapGet("/access/requests/pending", async (IAccessRequestService service, CancellationToken ct) =>
{
    var requests = await service.GetPendingRequestsAsync(ct);
    return Results.Ok(requests);
}).RequireAuthorization("Steward");

v1.MapPost("/access/requests/{requestId}/approve", async (string requestId, IAccessRequestService service, string approvedBy, CancellationToken ct) =>
{
    await service.ApproveRequestAsync(requestId, approvedBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Steward");

v1.MapPost("/access/requests/{requestId}/reject", async (string requestId, IAccessRequestService service, string rejectedBy, string reason, CancellationToken ct) =>
{
    await service.RejectRequestAsync(requestId, rejectedBy, reason, ct);
    return Results.NoContent();
}).RequireAuthorization("Steward");

v1.MapPost("/access/requests/{requestId}/revoke", async (string requestId, IAccessRequestService service, string revokedBy, CancellationToken ct) =>
{
    await service.RevokeAccessAsync(requestId, revokedBy, ct);
    return Results.NoContent();
}).RequireAuthorization("Steward");

v1.MapPost("/access/requests/expire", async (IAccessRequestService service, CancellationToken ct) =>
{
    var expired = await service.ExpireAccessAsync(ct);
    return Results.Ok(expired);
}).RequireAuthorization("Admin");

v1.MapGet("/access/requests/history", async (IAccessRequestService service, string? datasetId, string? requestedBy, CancellationToken ct) =>
{
    var history = await service.GetRequestHistoryAsync(datasetId, requestedBy, ct);
    return Results.Ok(history);
});

// Lineage & Impact Assessment
v1.MapPost("/lineage/assess/{breachId}", async (string breachId, ILineageService service, CancellationToken ct) =>
{
    var assessment = await service.AssessImpactAsync(breachId, ct);
    return Results.Ok(assessment);
}).RequireAuthorization("Steward");

v1.MapGet("/lineage/dataset/{datasetId}", async (string datasetId, ILineageService service, CancellationToken ct) =>
{
    var lineage = await service.GetLineageAsync(datasetId, ct);
    return Results.Ok(lineage);
});

v1.MapPost("/lineage", async (DataLineage lineage, ILineageService service, CancellationToken ct) =>
{
    await service.RecordLineageAsync(lineage, ct);
    return Results.Created($"/api/v1/lineage/{lineage.Id}", lineage);
}).RequireAuthorization("Steward");

// Steward Dashboard
v1.MapGet("/steward/dashboard/{stewardId}", async (string stewardId, IStewardDashboard dashboard, CancellationToken ct) =>
{
    var data = await dashboard.GetDashboardAsync(stewardId, ct);
    return Results.Ok(data);
}).RequireAuthorization("Steward");

v1.MapGet("/steward/trends", async (IStewardDashboard dashboard, DateTimeOffset? fromDate, DateTimeOffset? toDate, CancellationToken ct) =>
{
    var trends = await dashboard.GetBreachTrendsAsync(fromDate, toDate, ct);
    return Results.Ok(trends);
}).RequireAuthorization("Steward");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "data-governance" }));

app.Run();

public partial class Program { }


