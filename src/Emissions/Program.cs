using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
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
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddConsoleExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddConsoleExporter());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory stores
builder.Services.AddSingleton<IOC.Emissions.FactorsStore>();
builder.Services.AddSingleton<IOC.Emissions.LedgerStore>();
builder.Services.AddSingleton<IOC.Emissions.EmissionsEngine>();
builder.Services.AddSingleton<IOC.Emissions.ReadingStore>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ingest readings (flare, fuel, vent, LDAR)
app.MapPost("/emissions/ingest", (IOC.Emissions.RawReading reading, IOC.Emissions.LedgerStore ledger, IOC.Emissions.ReadingStore store) =>
{
    store.Add(reading);
    ledger.Append("reading", System.Text.Json.JsonSerializer.Serialize(reading));
    return Results.Accepted();
}).RequireAuthorization();

// Compute hourly/day/month aggregates
app.MapPost("/emissions/compute", (IOC.Emissions.ComputeRequest req, IOC.Emissions.EmissionsEngine engine, IOC.Emissions.LedgerStore ledger) =>
{
    var result = engine.Compute(req);
    ledger.Append("compute", System.Text.Json.JsonSerializer.Serialize(result));
    return Results.Ok(result);
}).RequireAuthorization();

// Factor edit approval and versioning
app.MapPost("/emissions/factors", (IOC.Emissions.FactorDefinition f, IOC.Emissions.FactorsStore store, IOC.Emissions.LedgerStore ledger) =>
{
    store.AddFactor(f);
    ledger.Append("factor", System.Text.Json.JsonSerializer.Serialize(f));
    return Results.Created($"/emissions/factors/{f.Code}:{f.Version}", f);
}).RequireAuthorization();

// CSV report (regulatory-ready minimal stub)
app.MapGet("/emissions/report.csv", (DateOnly from, DateOnly to, IOC.Emissions.EmissionsEngine engine) =>
{
    var csv = engine.GenerateCsv(from, to);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "emissions.csv");
}).RequireAuthorization();

// Ledger
app.MapGet("/emissions/ledger", (IOC.Emissions.LedgerStore ledger) => Results.Ok(ledger.All()));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }


