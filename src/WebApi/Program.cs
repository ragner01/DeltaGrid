using Asp.Versioning;
using FluentValidation;
using Hellang.Middleware.ProblemDetails;
using IOC.Application.Work.CreateWorkOrder;
using IOC.Application.Well.AdjustChoke;
using IOC.Application.Well.ChangeState;
using IOC.Application.Allocation;
using IOC.Application.Allocation.RunAllocation;
using IOC.Application.Allocation.Reconcile;
using IOC.Application.PTW;
using IOC.Application.Integrity;
using IOC.Application.Pipeline;
using IOC.Core.Domain.Pipeline;
using IOC.Application.Custody;
using IOC.Core.Domain.Custody;
using IOC.Application.Lab;
using IOC.Application.Twin;
using IOC.Infrastructure.Persistence;
using IOC.Security;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using IOC.WebApi.Hubs;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics.Metrics;
using System.Text;
using IOC.WebApi.Security;
using System.Security.Cryptography;

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
    opt.AddPolicy("AllocationApprover", policy =>
    {
        policy.RequireRole(Roles.Admin, Roles.ProductionEngineer);
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, AbacHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, TwinPathHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TwinPathScope", policy => policy.Requirements.Add(new TwinPathRequirement()));
});

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails(opts =>
{
    opts.IncludeExceptionDetails = (ctx, ex) => false;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddMediatR(typeof(IOC.Application.Work.CreateWorkOrder.CreateWorkOrderHandler).Assembly);

builder.Services.AddValidatorsFromAssemblyContaining<IOC.Application.Work.CreateWorkOrder.CreateWorkOrderValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<IOC.Application.Well.AdjustChoke.AdjustChokeValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<IOC.Application.Allocation.RunAllocation.RunAllocationValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<IOC.Application.Work.CreateWorkOrder.CreateWorkOrderCommand>();

builder.Services.AddSingleton<IWorkOrderRepository, InMemoryWorkOrderRepository>();
builder.Services.AddSingleton<IOC.Application.Well.IWellRepository, InMemoryWellRepository>();
builder.Services.AddSingleton<IOC.Application.Common.Outbox.IOutboxStore, InMemoryOutboxStore>();

builder.Services.AddSingleton<IBatteryRepository, InMemoryBatteryRepository>();
builder.Services.AddSingleton<IMeterReadingRepository, InMemoryMeterReadingRepository>();
builder.Services.AddSingleton<IWellTestRepository, InMemoryWellTestRepository>();
builder.Services.AddSingleton<IAllocationRunRepository, InMemoryAllocationRunRepository>();
builder.Services.AddSingleton<IAllocationReadRepository, InMemoryAllocationRunRepository>();

builder.Services.AddSingleton<IPtwRepository, InMemoryPtwRepository>();
builder.Services.AddSingleton<IPermitArchive, InMemoryPermitArchive>();
builder.Services.AddSingleton<IIntegrityRepository, InMemoryIntegrityRepository>();
builder.Services.AddSingleton<IPipelineRepository, InMemoryPipelineRepository>();
builder.Services.AddSingleton<ILeakEventPublisher, NoOpLeakEventPublisher>();
builder.Services.AddSingleton<ICustodyRepository, InMemoryCustodyRepository>();
builder.Services.AddSingleton<ILabRepository, InMemoryLabRepository>();
builder.Services.AddSingleton<ILabPropertySink, DurableLabPropertySink>();
builder.Services.AddSingleton<IPdfSigner, HmacPdfSigner>();
builder.Services.AddSingleton<ITwinRepository, InMemoryTwinRepository>();
builder.Services.AddSingleton<IKpiProvider, NoOpKpiProvider>();

var app = builder.Build();

// Seed demo allocation data
InMemoryBatteryRepository.Seed("bat-1", "site-1", "asset-1", new[] { "well-a", "well-b", "well-c" });
var day = DateOnly.FromDateTime(DateTime.UtcNow.Date);
InMemoryMeterReadingRepository.Seed(new IOC.Core.Domain.Allocation.BatteryMeasurement("bat-1", day, 30.0, 0.0, 0.0));
InMemoryWellTestRepository.Seed(new IOC.Core.Domain.Allocation.WellTest("well-a", day, 10, 0, 0));
InMemoryWellTestRepository.Seed(new IOC.Core.Domain.Allocation.WellTest("well-b", day, 15, 0, 0));
InMemoryWellTestRepository.Seed(new IOC.Core.Domain.Allocation.WellTest("well-c", day, 5, 0, 0));

app.UseSerilogRequestLogging();
// ProblemDetails (RFC7807) already added via services; ensure middleware enabled
app.UseProblemDetails();

// PII scrubber for common fields
app.Use(async (ctx, next) =>
{
    ctx.Items["pii:mask"] = new Func<string, string>(s => string.IsNullOrWhiteSpace(s) ? s : "***");
    await next();
});

var meter = new Meter("IOC.Auth", "1.0.0");
var authSuccess = meter.CreateCounter<int>("auth_success");
var authFailure = meter.CreateCounter<int>("auth_failure");
var varianceGauge = new Meter("IOC.Allocation", "1.0.0").CreateObservableGauge("allocation_variance_pct", () => new Measurement<double>[] { });

app.Use(async (ctx, next) =>
{
    await next();
    if (ctx.User?.Identity?.IsAuthenticated == true) authSuccess.Add(1);
});

app.UseAuthentication();
app.Use(async (ctx, next) =>
{
    if (ctx.Response.StatusCode == 401 || ctx.Response.StatusCode == 403)
    {
        authFailure.Add(1);
    }
    await next();
});
app.UseAuthorization();

// Signing middleware (HMAC over body for critical writes)
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/v1/critical") && string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        ctx.Request.Body.Position = 0;
        var sig = ctx.Request.Headers["X-Signature"].ToString();
        var key = builder.Configuration["SigningKey"] ?? "demo-sign-key";
        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(key));
        var expected = BitConverter.ToString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body))).Replace("-", "").ToLowerInvariant();
        if (!string.Equals(sig, expected, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("invalid signature"); return;
        }
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

v1.MapPost("/work/orders", async (IOC.Application.Work.CreateWorkOrder.CreateWorkOrderCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess
        ? Results.Created($"/api/v1/work/orders/{result.Value!.Id}", result.Value)
        : Results.ValidationProblem(new Dictionary<string, string[]>{{"error", new[]{result.Error ?? "unknown"}}});
});

v1.MapPost("/wells/{wellId:guid}/choke", async (Guid wellId, [FromBody] AdjustChokeCommand body, ISender sender, CancellationToken ct) =>
{
    var cmd = body with { WellId = wellId };
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/wells/{wellId:guid}/state", async (Guid wellId, [FromBody] IOC.Application.Well.ChangeState.ChangeWellStateCommand body, ISender sender, CancellationToken ct) =>
{
    var cmd = body with { WellId = wellId };
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/allocation/run", async ([FromBody] RunAllocationCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Accepted($"/api/v1/allocation/runs/{result.Value}") : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization("AllocationApprover");

v1.MapPost("/allocation/rerun", async ([FromBody] RunAllocationCommand cmd, ISender sender, CancellationToken ct) =>
{
    // rerun is the same command, version increments deterministically
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Accepted($"/api/v1/allocation/runs/{result.Value}") : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization("AllocationApprover");

v1.MapPost("/allocation/reconcile", async ([FromBody] ReconcileAllocationCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
});

v1.MapGet("/allocation/export/sample.csv", () =>
{
    var csv = new StringBuilder();
    csv.AppendLine("WellId,Day,Oil_m3,Gas_m3,Water_m3,Method,Version");
    csv.AppendLine("well-1,2025-10-30,10.000,100.000,2.000,ProportionalByTest,1");
    return Results.Text(csv.ToString(), "text/csv", Encoding.UTF8);
});

v1.MapPost("/ptw/workorders", async (IOC.Application.PTW.CreateWorkOrderCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Created($"/api/v1/ptw/workorders/{result.Value}", new { id = result.Value }) : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/ptw/permits", async (CreatePermitCommand cmd, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.Created($"/api/v1/ptw/permits/{result.Value}", new { id = result.Value }) : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/ptw/permits/{id:guid}/approve", async (Guid id, [FromBody] ApprovePermitCommand body, ISender sender, CancellationToken ct) =>
{
    var cmd = body with { PermitId = id };
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/ptw/permits/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ActivatePermitCommand(id), ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

v1.MapPost("/ptw/permits/{id:guid}/close", async (Guid id, [FromBody] ClosePermitCommand body, ISender sender, CancellationToken ct) =>
{
    var cmd = body with { PermitId = id };
    var result = await sender.Send(cmd, ct);
    return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
});

// Integrity endpoints
v1.MapPost("/integrity/readings", async ([FromBody] RecordThicknessCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

v1.MapGet("/integrity/corrosion", async ([FromQuery] string equipmentId, [FromQuery] string location, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new ComputeCorrosionRateQuery(equipmentId, location), ct);
    return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
});

v1.MapPost("/integrity/plans", async ([FromBody] CreateInspectionPlanCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

v1.MapPost("/integrity/anomalies", async ([FromBody] CreateAnomalyCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.Created($"/api/v1/integrity/anomalies/{r.Value}", new { id = r.Value }) : Results.BadRequest(new { error = r.Error });
});

v1.MapPost("/integrity/anomalies/{id:guid}/close", async (Guid id, [FromBody] string mitigation, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new CloseAnomalyCommand(id, mitigation), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Pipeline calibration
v1.MapPost("/pipeline/{segmentId}/calibrate", async (string segmentId, [FromBody] List<double> balances, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new CalibrateSegmentCommand(segmentId, balances), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Leak detect
v1.MapPost("/pipeline/{segmentId}/detect", async (string segmentId, [FromBody] DetectLeakBody body, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new DetectLeakCommand(segmentId, body.Upstream_m3_s, body.Downstream_m3_s, new MeterUncertainty(body.UpMeterId, body.UpUncertaintyPct), new MeterUncertainty(body.DnMeterId, body.DnUncertaintyPct), body.ElevationDelta_m, body.Temperature_C, DateTimeOffset.UtcNow), ct);
    return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
});

// Custody: register meter
v1.MapPost("/custody/meters", async ([FromBody] IOC.Application.Custody.Commands.RegisterMeterCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Custody: register prover
v1.MapPost("/custody/provers", async ([FromBody] IOC.Application.Custody.Commands.RegisterProverCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Custody: run proving
v1.MapPost("/custody/proving", async ([FromBody] ProvingRunInput input, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Custody.Commands.RunProvingCommand(input), ct);
    return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
});

// Custody: generate ticket
v1.MapPost("/custody/tickets", async ([FromBody] IOC.Application.Custody.Commands.GenerateTicketCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.Ok(new { ticketNumber = r.Value }) : Results.BadRequest(new { error = r.Error });
});

// Custody: approve ticket
v1.MapPost("/custody/tickets/{ticketNumber}/approve", async (string ticketNumber, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Custody.Commands.ApproveTicketCommand(ticketNumber), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: plan sample
v1.MapPost("/lab/samples/plan", async ([FromBody] IOC.Application.Lab.Commands.PlanSampleCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: mark collected
v1.MapPost("/lab/samples/{sampleId}/collect", async (string sampleId, [FromBody] DateTimeOffset collectedAt, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Lab.Commands.MarkCollectedCommand(sampleId, collectedAt, "field"), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: receive sample
v1.MapPost("/lab/samples/{sampleId}/receive", async (string sampleId, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Lab.Commands.ReceiveSampleCommand(sampleId, "lab"), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: record result
v1.MapPost("/lab/results", async ([FromBody] IOC.Application.Lab.Commands.RecordLabResultCommand cmd, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: set quality flag
v1.MapPost("/lab/results/{sampleId}/quality", async (string sampleId, [FromBody] string flag, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Lab.Commands.SetQualityFlagCommand(sampleId, flag, "lab"), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: request retest
v1.MapPost("/lab/samples/{sampleId}/retest", async (string sampleId, [FromBody] string reason, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Lab.Commands.RequestRetestCommand(sampleId, reason, "ops"), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: push properties
v1.MapPost("/lab/results/{sampleId}/push", async (string sampleId, ISender sender, CancellationToken ct) =>
{
    var r = await sender.Send(new IOC.Application.Lab.Commands.PushPropertiesCommand(sampleId), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
});

// Lab: read last pushed properties
v1.MapGet("/lab/properties/{sourceId}", (string sourceId) =>
{
    return DurableLabPropertySink.TryGet(sourceId, out var val)
        ? Results.Ok(new { sourceId, val.api, val.gor, val.wc, val.viscosity, val.ts })
        : Results.NotFound();
});

// Helper to enforce path prefix from claims
bool IsAllowedPath(HttpContext http, string idPath)
{
    var prefix = http.User.FindFirst("twin:path")?.Value ?? "/";
    return idPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}

// Twin: import CSV (each line: IdPath,Level,Name)
v1.MapPost("/twin/import", async (HttpContext http, [FromBody] List<string> csvLines, ISender sender, CancellationToken ct) =>
{
    // Basic path-scope guard: all lines must start with allowed prefix
    var prefix = http.User.FindFirst("twin:path")?.Value ?? "/";
    if (csvLines.Any(l => !string.IsNullOrWhiteSpace(l) && !l.Split(',')[0].Trim().StartsWith(prefix)))
        return Results.Forbid();
    var r = await sender.Send(new IOC.Application.Twin.Commands.ImportHierarchyCommand(csvLines), ct);
    return r.IsSuccess ? Results.Ok(new { version = r.Value }) : Results.BadRequest(new { error = r.Error });
}).RequireAuthorization("TwinPathScope");

// Twin: upsert node
v1.MapPost("/twin/nodes", async (HttpContext http, [FromBody] IOC.Application.Twin.Commands.UpsertNodeCommand cmd, ISender sender, CancellationToken ct) =>
{
    if (!IsAllowedPath(http, cmd.IdPath)) return Results.Forbid();
    var r = await sender.Send(cmd, ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
}).RequireAuthorization("TwinPathScope");

// Twin: soft delete
v1.MapDelete("/twin/nodes", async (HttpContext http, [FromQuery] string idPath, ISender sender, CancellationToken ct) =>
{
    if (!IsAllowedPath(http, idPath)) return Results.Forbid();
    var r = await sender.Send(new IOC.Application.Twin.Commands.SoftDeleteNodeCommand(idPath), ct);
    return r.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
}).RequireAuthorization("TwinPathScope");

// Twin: snapshot
v1.MapGet("/twin/snapshot", async (HttpContext http, [FromQuery] string idPath, ISender sender, CancellationToken ct) =>
{
    if (!IsAllowedPath(http, idPath)) return Results.Forbid();
    var r = await sender.Send(new IOC.Application.Twin.Commands.SnapshotQuery(idPath), ct);
    return r.IsSuccess ? Results.Ok(r.Value) : Results.NotFound(new { error = r.Error });
}).RequireAuthorization("TwinPathScope");

// Twin: impact analysis
v1.MapGet("/twin/impact", async (HttpContext http, [FromQuery] string idPath, [FromQuery] string relation, ISender sender, CancellationToken ct) =>
{
    if (!IsAllowedPath(http, idPath)) return Results.Forbid();
    var r = await sender.Send(new IOC.Application.Twin.Commands.ImpactQuery(idPath, relation), ct);
    return r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(new { error = r.Error });
}).RequireAuthorization("TwinPathScope");

// Ops actions
v1.MapPost("/ops/ack", async ([FromServices] IHubContext<OpsHub> hub, [FromBody] Guid targetId) =>
{
    await hub.Clients.All.SendAsync("msg", $"ACK {targetId}");
    return Results.NoContent();
});

v1.MapPost("/ops/shelve", async ([FromServices] IHubContext<OpsHub> hub, [FromBody] Guid targetId) =>
{
    await hub.Clients.All.SendAsync("msg", $"SHELVED {targetId}");
    return Results.NoContent();
});

v1.MapPost("/ops/workorders", async ([FromServices] IHubContext<OpsHub> hub, [FromBody] Guid targetId) =>
{
    await hub.Clients.All.SendAsync("msg", $"WO-CREATED {targetId}");
    return Results.Ok(new { workOrderId = Guid.NewGuid() });
});

app.MapHub<OpsHub>("/opsHub");

app.Run();

public sealed record DetectLeakBody(string UpMeterId, double Upstream_m3_s, double UpUncertaintyPct, string DnMeterId, double Downstream_m3_s, double DnUncertaintyPct, double ElevationDelta_m, double Temperature_C);

public partial class Program {}
