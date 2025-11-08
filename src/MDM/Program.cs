using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = false,
        ValidateLifetime = true,
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory MDM stores
builder.Services.AddSingleton<IOC.MDM.AuthorityRegistry>();
builder.Services.AddSingleton<IOC.MDM.ReferenceStore>();
builder.Services.AddSingleton<IOC.MDM.GoldenRecordStore>();
builder.Services.AddSingleton<IOC.MDM.SnapshotStore>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Reference lookups
app.MapGet("/mdm/units", (IOC.MDM.ReferenceStore refs) => Results.Ok(refs.Units()));
app.MapGet("/mdm/codes", (IOC.MDM.ReferenceStore refs) => Results.Ok(refs.Codes()));

// Bulk import and snapshot export
app.MapPost("/mdm/import", (List<IOC.MDM.MasterRecord> records, IOC.MDM.GoldenRecordStore gr, IOC.MDM.SnapshotStore snaps) =>
{
    foreach (var r in records) gr.Upsert(r);
    var snap = snaps.CreateSnapshot();
    return Results.Ok(new { snapshotId = snap.Id, count = records.Count });
}).RequireAuthorization();

app.MapGet("/mdm/snapshots/{id}", (string id, IOC.MDM.SnapshotStore snaps) =>
{
    return snaps.TryGet(id, out var s) ? Results.Ok(s) : Results.NotFound();
});

// Diff and audit
app.MapGet("/mdm/diff", (string fromId, string toId, IOC.MDM.SnapshotStore snaps) =>
{
    var diff = snaps.Diff(fromId, toId);
    return Results.Ok(diff);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }


