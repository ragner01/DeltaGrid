# Quick Security Fixes - Developer Guide

This document provides copy-paste code fixes for critical security vulnerabilities.

## 1. Fix JWT Validation (CRITICAL)

### Replace in ALL Program.cs files:

**BEFORE (VULNERABLE):**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,  // ⚠️ DANGEROUS!
            ValidateLifetime = true
        };
    });
```

**AFTER (SECURE):**
```csharp
// Add to services first
builder.Services.AddSingleton<IKeyVaultSecretManager>(sp =>
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"] 
        ?? throw new InvalidOperationException("KeyVault:Url not configured");
    var logger = sp.GetRequiredService<ILogger<KeyVaultSecretManager>>();
    return new KeyVaultSecretManager(keyVaultUrl, logger);
});

builder.Services.AddSingleton<IJwtValidator>(sp =>
{
    var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
    var issuer = builder.Configuration["JWT:Issuer"] ?? "https://deltagrid.io";
    var audience = builder.Configuration["JWT:Audience"] ?? "deltagrid-api";
    var signingKeyName = builder.Configuration["JWT:SigningKeyName"] ?? "jwt-signing-key";
    var logger = sp.GetRequiredService<ILogger<JwtValidator>>();
    return new JwtValidator(keyVault, issuer, audience, signingKeyName, logger);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "https://deltagrid.io",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "deltagrid-api",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            RequireAudience = true
        };
        
        // Get signing key from Key Vault
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                var keyVault = context.HttpContext.RequestServices
                    .GetRequiredService<IKeyVaultSecretManager>();
                var signingKeyName = builder.Configuration["JWT:SigningKeyName"] ?? "jwt-signing-key";
                var signingKey = await keyVault.GetSecretAsync(signingKeyName);
                var keyBytes = Convert.FromBase64String(signingKey);
                context.TokenValidationParameters.IssuerSigningKey = 
                    new SymmetricSecurityKey(keyBytes);
            }
        };
    });
```

---

## 2. Fix SQL Injection in Azure Search

**File:** `src/Search/Querying/AzureSearchService.cs`

**BEFORE (VULNERABLE):**
```csharp
if (!string.IsNullOrEmpty(query.TenantId))
{
    filters.Add($"tenantId eq '{query.TenantId}'");  // ⚠️ INJECTION
}
```

**AFTER (SECURE):**
```csharp
private static string EscapeODataString(string input)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    return input.Replace("'", "''")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace("?", "\\?")
                .Replace("#", "\\#")
                .Replace("&", "\\&");
}

// In SearchAsync method:
if (!string.IsNullOrEmpty(query.TenantId))
{
    var escapedTenantId = EscapeODataString(query.TenantId);
    filters.Add($"tenantId eq '{escapedTenantId}'");
}

if (query.RequiredRoles != null && query.RequiredRoles.Any())
{
    var roleFilters = query.RequiredRoles
        .Select(r => $"roles/any(role: role eq '{EscapeODataString(r)}')");
    filters.Add($"({string.Join(" or ", roleFilters)})");
}
```

---

## 3. Fix KQL Injection

**File:** `src/TimeSeries/AdxClient.cs`

**BEFORE (VULNERABLE):**
```csharp
public Task<IDataReader> QueryAsync(string kql, ClientRequestProperties? props = null)
    => Task.FromResult(_query.ExecuteQuery(_database, kql, props));
```

**AFTER (SECURE):**
```csharp
private static readonly HashSet<string> AllowedKqlOperations = new()
{
    "where", "project", "extend", "summarize", "take", "limit",
    "order", "sort", "top", "count", "distinct"
};

private static readonly HashSet<string> BlockedKqlOperations = new()
{
    "union", "join", "database(", ".execute", ".create", ".alter", ".drop",
    ".set", ".append", ".replace", ".delete", ".move", ".rename"
};

public Task<IDataReader> QueryAsync(string kql, ClientRequestProperties? props = null)
{
    if (string.IsNullOrWhiteSpace(kql))
        throw new ArgumentException("KQL query cannot be empty", nameof(kql));
    
    if (!IsKqlSafe(kql))
        throw new SecurityException($"Unsafe KQL query detected: {kql}");
    
    return Task.FromResult(_query.ExecuteQuery(_database, kql, props));
}

private static bool IsKqlSafe(string kql)
{
    var lowerKql = kql.ToLowerInvariant();
    
    // Block dangerous operations
    if (BlockedKqlOperations.Any(op => lowerKql.Contains(op)))
        return false;
    
    // Validate query structure (basic check)
    if (lowerKql.Contains("..") || lowerKql.Contains("//"))
        return false;
    
    return true;
}
```

---

## 4. Fix Hardcoded Secrets

**File:** `src/Identity/Program.cs`

**BEFORE (VULNERABLE):**
```csharp
ClientSecrets = { new Secret("secret".Sha256()) },  // ⚠️ HARDCODED
```

**AFTER (SECURE):**
```csharp
// In SeedAsync method, inject KeyVaultSecretManager
static async Task SeedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var keyVault = scope.ServiceProvider.GetRequiredService<IKeyVaultSecretManager>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    // Get secret from Key Vault
    var gatewaySecret = await keyVault.GetSecretAsync("identity-svc-gateway-secret");
    
    // Update client configuration
    var clients = new List<Client>
    {
        new Client
        {
            ClientId = "svc-gateway",
            ClientSecrets = { new Secret(gatewaySecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" }
        }
    };
    
    // Re-register IdentityServer with updated clients
    // (This requires refactoring to use IConfiguration or factory pattern)
}
```

**Better approach - Use Configuration:**
```csharp
// In Program.cs, load clients from config/Key Vault
builder.Services.AddIdentityServer()
    .AddInMemoryClients(() =>
    {
        var keyVault = serviceProvider.GetRequiredService<IKeyVaultSecretManager>();
        var gatewaySecret = keyVault.GetSecretAsync("identity-svc-gateway-secret").Result;
        
        return new[]
        {
            new Client
            {
                ClientId = "svc-gateway",
                ClientSecrets = { new Secret(gatewaySecret.Sha256()) },
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = { "api" }
            }
        };
    });
```

---

## 5. Fix Idempotency Store (DoS)

**File:** `src/Gateway/Program.cs`

**BEFORE (VULNERABLE):**
```csharp
var idempotencyStore = new Dictionary<string, DateTimeOffset>();
```

**AFTER (SECURE):**
```csharp
// Add Redis to services
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"] 
        ?? throw new InvalidOperationException("Redis:ConnectionString not configured");
    options.InstanceName = "IOC:";
});

// Replace middleware
app.Use(async (ctx, next) =>
{
    if (string.Equals(ctx.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ctx.Request.Method, "PUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ctx.Request.Method, "PATCH", StringComparison.OrdinalIgnoreCase))
    {
        var cache = ctx.RequestServices.GetRequiredService<IDistributedCache>();
        var key = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
        
        if (!string.IsNullOrWhiteSpace(key))
        {
            // Limit key length to prevent abuse
            if (key.Length > 256)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("Idempotency-Key too long");
                return;
            }
            
            var cacheKey = $"idempotency:{key}";
            var existing = await cache.GetStringAsync(cacheKey);
            
            if (existing != null)
            {
                ctx.Response.StatusCode = 409;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Duplicate request\"}");
                return;
            }
            
            // Store for 24 hours
            await cache.SetStringAsync(cacheKey, "processed", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
        }
    }
    
    await next();
});
```

---

## 6. Add Input Validation

**File:** `src/Optimization/Program.cs`

**Add NuGet package:**
```xml
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

**Add validator:**
```csharp
// Create file: src/Optimization/Validators/OptimizeRequestValidator.cs
using FluentValidation;
using IOC.Optimization;

namespace IOC.Optimization.Validators;

public class OptimizeRequestValidator : AbstractValidator<OptimizeRequest>
{
    public OptimizeRequestValidator()
    {
        RuleFor(x => x.WellId)
            .NotEmpty()
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("WellId must contain only alphanumeric characters, hyphens, and underscores")
            .MaximumLength(100);
        
        RuleFor(x => x.LiftMethod)
            .NotEmpty()
            .Must(m => m == "GasLift" || m == "ESP")
            .WithMessage("LiftMethod must be either 'GasLift' or 'ESP'");
        
        RuleFor(x => x.Window)
            .NotEmpty()
            .WithMessage("Window cannot be empty")
            .Must(w => w.Count > 0 && w.Count <= 1000)
            .WithMessage("Window size must be between 1 and 1000");
        
        RuleFor(x => x.Constraints)
            .NotNull()
            .WithMessage("Constraints cannot be null");
        
        RuleFor(x => x.Constraints.MinChokePct)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100)
            .LessThan(x => x.Constraints.MaxChokePct)
            .WithMessage("MinChokePct must be between 0 and 100, and less than MaxChokePct");
        
        RuleFor(x => x.Constraints.MaxChokePct)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100)
            .WithMessage("MaxChokePct must be between 0 and 100");
        
        RuleFor(x => x.Constraints.MinPressurePa)
            .LessThan(x => x.Constraints.MaxPressurePa)
            .WithMessage("MinPressurePa must be less than MaxPressurePa");
    }
}
```

**Update Program.cs:**
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<OptimizeRequestValidator>();

app.MapPost("/optimize", async (
    [FromBody] OptimizeRequest req,
    IValidator<OptimizeRequest> validator,
    RulesEngine rules,
    OnnxSurrogate onnx) =>
{
    var validationResult = await validator.ValidateAsync(req);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = 400,
            Detail = "Request validation failed",
            Extensions = { ["errors"] = validationResult.Errors }
        });
    }
    
    // Existing handler code...
});
```

---

## 7. Add Security Headers Middleware

**Create:** `src/BuildingBlocks/Security/SecurityHeadersMiddleware.cs`

```csharp
namespace IOC.BuildingBlocks.Security;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS - Force HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = 
                "max-age=31536000; includeSubDomains; preload";
        }

        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // XSS Protection (legacy but still useful)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';";

        // Permissions Policy
        context.Response.Headers["Permissions-Policy"] = 
            "geolocation=(), microphone=(), camera=(), payment=(), usb=()";

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

**Use in all Program.cs files:**
```csharp
app.UseSecurityHeaders();
```

---

## 8. Add Request Size Limits

**Add to all Program.cs files:**
```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10_485_760; // 10MB
    options.ValueLengthLimit = 4_194_304; // 4MB
    options.ValueCountLimit = 100;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.MaxDepth = 32;
});

// Add middleware
app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering();
    
    if (ctx.Request.ContentLength > 10_485_760) // 10MB
    {
        ctx.Response.StatusCode = 413; // Payload Too Large
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(
            "{\"error\":\"Request payload exceeds maximum size of 10MB\"}");
        return;
    }
    
    await next();
});
```

---

## 9. Fix Logging (Remove Sensitive Data)

**Update Serilog configuration:**
```csharp
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
    .Filter.ByExcluding(logEvent =>
    {
        // Exclude sensitive properties
        return logEvent.Properties.Any(p => 
            p.Key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            p.Key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
            p.Key.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
            p.Key.Contains("Key", StringComparison.OrdinalIgnoreCase));
    })
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day));
```

**Update KeyVaultSecretManager logging:**
```csharp
// BEFORE
_logger.LogInformation("Retrieved secret {SecretName} from Key Vault", secretName);

// AFTER
_logger.LogInformation("Retrieved secret from Key Vault", 
    new { SecretName = MaskSensitive(secretName) });

private static string MaskSensitive(string input)
{
    if (string.IsNullOrEmpty(input)) return "***";
    if (input.Length <= 4) return "****";
    return input.Substring(0, 2) + "****" + input.Substring(input.Length - 2);
}
```

---

## 10. Add CORS Configuration

**Add to all Program.cs files:**
```csharp
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
            .WithExposedHeaders("X-Correlation-ID", "X-Request-ID")
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// Use CORS
app.UseCors("AllowedOrigins");
```

---

## Testing Your Fixes

### Test JWT Validation:
```bash
# Should fail - invalid signature
curl -H "Authorization: Bearer invalid-token" https://api/endpoint

# Should fail - missing signature
curl -H "Authorization: Bearer eyJhbGciOiJub25lIn0.eyJzdWIiOiJhZG1pbiJ9." https://api/endpoint
```

### Test SQL Injection:
```bash
# Should be escaped, not execute
curl -X POST https://api/search \
  -d '{"tenantId": "tenant-a'\'' or '\''1'\''='\''1"}'
```

### Test Rate Limiting:
```bash
# Send 201 requests rapidly - should get 429
for i in {1..201}; do curl https://api/endpoint; done
```

---

## Priority Order

1. **Fix JWT validation** (1 day) - CRITICAL
2. **Fix SQL/KQL injection** (2 days) - CRITICAL  
3. **Fix hardcoded secrets** (1 day) - CRITICAL
4. **Add input validation** (3 days) - CRITICAL
5. **Fix idempotency store** (1 day) - CRITICAL
6. **Add security headers** (1 day) - HIGH
7. **Add CORS** (1 day) - HIGH
8. **Fix logging** (2 days) - HIGH
9. **Add request limits** (1 day) - HIGH
10. **Add audit logging** (3 days) - MEDIUM

**Total estimated time: 2-3 weeks**

