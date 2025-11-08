using IOC.Optimization;
using IOC.Optimization.Inference;
using IOC.Optimization.Rules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddGrpc();

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OptimizationExecutor", p => p.RequireRole("ProductionEngineer", "Admin"));

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RulesEngine>();

// Model path and checksum from config; leave placeholders for now
string modelPath = builder.Configuration["Optimization:Onnx:Path"] ?? "models/surrogate.onnx";
string sha = builder.Configuration["Optimization:Onnx:Sha256"] ?? new string('0', 64);
builder.Services.AddSingleton(new OnnxSurrogate(modelPath, sha));

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGrpcService<OptimizerGrpcService>().RequireAuthorization("OptimizationExecutor");

app.MapPost("/optimize", (IOC.Optimization.OptimizeRequest req, RulesEngine rules, OnnxSurrogate onnx) =>
{
    // Convert gRPC types to tuples expected by RulesEngine
    IEnumerable<(DateTimeOffset ts, double pressurePa, double temperatureC, double flowM3s, double chokePct, double espFreqHz)> window = req.Window.Select(p =>
        (
            ts: DateTimeOffset.FromUnixTimeMilliseconds(p.TsUnixMs),
            pressurePa: p.PressurePa,
            temperatureC: p.TemperatureC,
            flowM3s: p.FlowM3S,
            chokePct: p.ChokePct,
            espFreqHz: p.EspFreqHz
        ));
    (double minChoke, double maxChoke, double minP, double maxP, double minT, double maxT) c = (
        req.Constraints.MinChokePct,
        req.Constraints.MaxChokePct,
        req.Constraints.MinPressurePa,
        req.Constraints.MaxPressurePa,
        req.Constraints.MinTemperatureC,
        req.Constraints.MaxTemperatureC);
    (double chokePct, double espFreqHz, string rationale) result = rules.Recommend(req.LiftMethod, window, c);
    double rChoke = result.chokePct;
    double rEsp = result.espFreqHz;
    string rationaleRules = result.rationale;

    // simple features: last point concatenated with rule outputs
    TelemetryPoint? last = req.Window.LastOrDefault();
    if (last == null)
    {
        return Results.BadRequest("Window cannot be empty");
    }

    double[] feats = { last.PressurePa, last.TemperatureC, last.FlowM3S, last.ChokePct, last.EspFreqHz, rChoke, rEsp };
    (double, double, string) onnxResult = onnx.Predict(req.LiftMethod, feats);
    double mChoke = onnxResult.Item1;
    double mEsp = onnxResult.Item2;
    string rationaleOnnx = onnxResult.Item3;

    // blend: average with guardrails
    double choke = Math.Clamp((rChoke + mChoke) / 2.0, req.Constraints.MinChokePct, req.Constraints.MaxChokePct);
    double esp = Math.Max(0, (rEsp + mEsp) / 2.0);

    return Results.Ok(new IOC.Optimization.OptimizeResponse
    {
        WellId = req.WellId,
        LiftMethod = req.LiftMethod,
        RecommendedChokePct = Math.Round(choke, 2),
        RecommendedEspFreqHz = Math.Round(esp, 2),
        Rationale = $"{rationaleRules};  {rationaleOnnx}",
    });
}).RequireAuthorization("OptimizationExecutor");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

/// <summary>
/// Program entry point.
/// </summary>
public partial class Program
{
}
