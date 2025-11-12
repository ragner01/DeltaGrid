using IOC.Optimization;
using IOC.Optimization.Inference;
using IOC.Optimization.Rules;
using IOC.Optimization.Validators;
using IOC.Security.Jwt;
using IOC.Security.KeyVault;
using IOC.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Configure request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10_485_760; // 10MB
    options.ValueLengthLimit = 4_194_304; // 4MB
    options.ValueCountLimit = 100;
});

builder.Services.AddGrpc();

// Configure Key Vault Secret Manager
var keyVaultUrl = builder.Configuration["KeyVault:Url"] 
    ?? builder.Configuration["Azure:KeyVault:Url"]
    ?? throw new InvalidOperationException("KeyVault:Url must be configured");

builder.Services.AddSingleton<IKeyVaultSecretManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KeyVaultSecretManager>>();
    return new KeyVaultSecretManager(keyVaultUrl, logger);
});

// Configure JWT validation with proper security
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "https://deltagrid.io";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "deltagrid-api";
var jwtSigningKeyName = builder.Configuration["JWT:SigningKeyName"] ?? "jwt-signing-key";

builder.Services.AddSingleton<IJwtValidator>(sp =>
{
    var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
    var logger = sp.GetRequiredService<ILogger<JwtValidator>>();
    return new JwtValidator(keyVault, jwtIssuer, jwtAudience, jwtSigningKeyName, logger);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // Configure token validation parameters
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            RequireAudience = true
        };
    });

// Set signing key from Key Vault during configuration
builder.Services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<JwtBearerOptions>>(sp =>
{
    return new Microsoft.Extensions.Options.ConfigureNamedOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme, 
        options =>
        {
            try
            {
                var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
                var signingKey = keyVault.GetSecretAsync(jwtSigningKeyName).Result;
                var keyBytes = Convert.FromBase64String(signingKey);
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(keyBytes);
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to configure JWT signing key from Key Vault");
                throw;
            }
        });
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

// Add input validation
builder.Services.AddValidatorsFromAssemblyContaining<OptimizeRequestValidator>();

builder.Services.AddSingleton<RulesEngine>();

// Model path and checksum from config; validate path to prevent traversal
string modelPath = builder.Configuration["Optimization:Onnx:Path"] ?? "models/surrogate.onnx";
var fullPath = Path.GetFullPath(modelPath);
var baseDir = Path.GetFullPath("models");

if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
{
    throw new System.Security.SecurityException($"Model path '{modelPath}' is outside allowed directory '{baseDir}'");
}

if (!File.Exists(fullPath))
{
    throw new FileNotFoundException($"Model file not found: {fullPath}", fullPath);
}

string sha = builder.Configuration["Optimization:Onnx:Sha256"] ?? new string('0', 64);
builder.Services.AddSingleton(new OnnxSurrogate(fullPath, sha));

WebApplication app = builder.Build();

// Add security headers
app.UseSecurityHeaders();

// Add request size limit middleware
app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering();
    
    if (ctx.Request.ContentLength > 10_485_760) // 10MB
    {
        ctx.Response.StatusCode = 413; // Payload Too Large
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{\"error\":\"Request payload exceeds maximum size of 10MB\"}");
        return;
    }
    
    await next();
});

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Only enable Swagger in development and if explicitly enabled
if (app.Environment.IsDevelopment() && 
    builder.Configuration.GetValue<bool>("EnableSwagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Optimization API");
        options.RoutePrefix = string.Empty;
    });
}

app.MapGrpcService<OptimizerGrpcService>().RequireAuthorization("OptimizationExecutor");

app.MapPost("/optimize", async (
    [Microsoft.AspNetCore.Mvc.FromBody] OptimizeRequest req,
    IValidator<OptimizeRequest> validator,
    RulesEngine rules,
    OnnxSurrogate onnx) =>
{
    // Validate input
    var validationResult = await validator.ValidateAsync(req);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = 400,
            Detail = "Request validation failed",
            Extensions = { ["errors"] = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) }
        });
    }

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
    (double rChoke, double rEsp, string rationaleRules) = rules.Recommend(req.LiftMethod, window, c);

    // simple features: last point concatenated with rule outputs
    TelemetryPoint? last = req.Window.LastOrDefault();
    if (last == null)
    {
        return Results.BadRequest(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = 400,
            Detail = "Invalid request parameters"
        });
    }

    double[] feats = { last.PressurePa, last.TemperatureC, last.FlowM3S, last.ChokePct, last.EspFreqHz, rChoke, rEsp };
    (double, double, string) onnxResult = onnx.Predict(req.LiftMethod, feats);
    double mChoke = onnxResult.Item1;
    double mEsp = onnxResult.Item2;
    string rationaleOnnx = onnxResult.Item3;

    // blend: average with guardrails
    double choke = Math.Clamp((rChoke + mChoke) / 2.0, req.Constraints.MinChokePct, req.Constraints.MaxChokePct);
    double esp = Math.Max(0, (rEsp + mEsp) / 2.0);

    return Results.Ok(new OptimizeResponse
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
