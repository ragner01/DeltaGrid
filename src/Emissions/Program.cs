using IOC.Security.Jwt;
using IOC.Security.KeyVault;
using IOC.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Configure request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10_485_760; // 10MB
    options.ValueLengthLimit = 4_194_304; // 4MB
    options.ValueCountLimit = 100;
});

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
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
{
    return new ConfigureNamedOptions<JwtBearerOptions>(
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

// Configure CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "https://localhost:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Correlation-ID")
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Emissions API");
        options.RoutePrefix = string.Empty;
    });
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


