using IOC.Events;
using IOC.Security.Jwt;
using IOC.Security.KeyVault;
using IOC.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddSingleton<EventStore>();
var policy = new SuppressionPolicy { DedupWindow = TimeSpan.FromSeconds(10), ChatterWindow = TimeSpan.FromSeconds(30), FloodThreshold = 100, MaintenanceMode = false };
builder.Services.AddSingleton(policy);
builder.Services.AddSingleton<INotificationSink>(_ => new WebhookNotificationSink("http://localhost:9000/webhook"));
builder.Services.AddSingleton<Router>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IOC Events API");
        options.RoutePrefix = string.Empty;
    });
}

app.MapPost("/events/ingest", async (string tenant, string site, string asset, RawAlarm raw, Severity sev, Consequence cons, string category, int priority, bool shelved, Router router, CancellationToken ct) =>
{
    var e = await router.RouteAsync(tenant, site, asset, raw, sev, cons, category, priority, shelved, ct);
    return e is null ? Results.Accepted() : Results.Ok(e);
});

app.MapPost("/events/{id:guid}/ack", (Guid id, EventStore store) =>
{
    if (store.TryGet(id, out var e)) { store.Ack(id, DateTimeOffset.UtcNow); return Results.NoContent(); }
    return Results.NotFound();
});

app.MapGet("/events/timeline", (EventStore store) => Results.Ok(store.All()));

app.MapPost("/events/replay", async (string tenant, string site, string asset, List<RawAlarm> alarms, Router router, CancellationToken ct) =>
{
    foreach (var a in alarms)
    {
        await router.RouteAsync(tenant, site, asset, a, Severity.Medium, Consequence.Production, "Replay", 3, false, ct);
    }
    return Results.Accepted();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program {}
