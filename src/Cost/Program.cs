using IOC.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Configure security features
builder.Services.AddRequestSizeLimits();
builder.Services.AddSecureJwtAuthentication(builder.Configuration);
builder.Services.AddSecureCors(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddConsoleExporter());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IOC.Cost.CostStores>();
builder.Services.AddSingleton<IOC.Cost.FxRatesStore>();
builder.Services.AddSingleton<IOC.Cost.AllocationEngine>();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Cost API");
        options.RoutePrefix = string.Empty;
    });
}

// Ingest WO/materials/labor
app.MapPost("/cost/ingest/wo", (IOC.Cost.WorkOrder wo, IOC.Cost.CostStores s) => { s.AddWorkOrder(wo); return Results.Accepted(); })
    .RequireAuthorization();
app.MapPost("/cost/ingest/material", (IOC.Cost.MaterialUse m, IOC.Cost.CostStores s) => { s.AddMaterial(m); return Results.Accepted(); })
    .RequireAuthorization();
app.MapPost("/cost/ingest/labor", (IOC.Cost.LaborHour l, IOC.Cost.CostStores s) => { s.AddLabor(l); return Results.Accepted(); })
    .RequireAuthorization();

// Attribution run (period close)
app.MapPost("/cost/allocate", (IOC.Cost.AllocationRequest req, IOC.Cost.AllocationEngine eng) =>
{
    var run = eng.Run(req);
    return Results.Ok(run);
}).RequireAuthorization();

// Reconciliation view
app.MapGet("/cost/recon", (DateOnly period, IOC.Cost.AllocationEngine eng) => Results.Ok(eng.Reconcile(period)))
    .RequireAuthorization();

// Export package (CSV)
app.MapGet("/cost/export.csv", (DateOnly period, IOC.Cost.AllocationEngine eng) =>
{
    var csv = eng.Export(period);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"close_{period}.csv");
}).RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }


