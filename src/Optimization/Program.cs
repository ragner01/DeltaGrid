using IOC.Optimization;
using IOC.Optimization.Inference;
using IOC.Optimization.Rules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("OptimizationExecutor", p => p.RequireRole("ProductionEngineer", "Admin"));
});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RulesEngine>();

// Model path and checksum from config; leave placeholders for now
var modelPath = builder.Configuration["Optimization:Onnx:Path"] ?? "models/surrogate.onnx";
var sha = builder.Configuration["Optimization:Onnx:Sha256"] ?? new string('0', 64);
builder.Services.AddSingleton(new OnnxSurrogate(modelPath, sha));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGrpcService<OptimizerGrpcService>().RequireAuthorization("OptimizationExecutor");

app.MapPost("/optimize", (OptimizeRequest req, RulesEngine rules, OnnxSurrogate onnx) =>
{
    var window = req.Window.Select(p => (DateTimeOffset.FromUnixTimeMilliseconds(p.TsUnixMs), p.PressurePa, p.TemperatureC, p.FlowM3S, p.ChokePct, p.EspFreqHz));
    var c = (req.Constraints.MinChokePct, req.Constraints.MaxChokePct, req.Constraints.MinPressurePa, req.Constraints.MaxPressurePa, req.Constraints.MinTemperatureC, req.Constraints.MaxTemperatureC);
    var (rChoke, rEsp, rationaleRules) = rules.Recommend(req.LiftMethod, window, c);
    // simple features: last point concatenated with rule outputs
    var last = req.Window.LastOrDefault();
    var feats = new double[] { last.PressurePa, last.TemperatureC, last.FlowM3S, last.ChokePct, last.EspFreqHz, rChoke, rEsp };
    var (mChoke, mEsp, rationaleOnnx) = onnx.Predict(req.LiftMethod, feats);

    // blend: average with guardrails
    double choke = Math.Clamp((rChoke + mChoke) / 2.0, req.Constraints.MinChokePct, req.Constraints.MaxChokePct);
    double esp = Math.Max(0, (rEsp + mEsp) / 2.0);

    return Results.Ok(new OptimizeResponse
    {
        WellId = req.WellId,
        LiftMethod = req.LiftMethod,
        RecommendedChokePct = Math.Round(choke, 2),
        RecommendedEspFreqHz = Math.Round(esp, 2),
        Rationale = $"{rationaleRules}; {rationaleOnnx}"
    });
}).RequireAuthorization("OptimizationExecutor");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program {}
