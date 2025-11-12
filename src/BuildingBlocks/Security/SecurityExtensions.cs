using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using IOC.Security.Jwt;
using IOC.Security.KeyVault;

namespace IOC.BuildingBlocks.Security;

/// <summary>
/// Extension methods for configuring security features
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Configure secure JWT authentication with Key Vault signing key
    /// </summary>
    public static IServiceCollection AddSecureJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keyVaultUrl = configuration["KeyVault:Url"] 
            ?? configuration["Azure:KeyVault:Url"];
        
        // For test environments, use a mock Key Vault if URL is not configured
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["Environment"] ?? "Production";
            if (env == "Test" || env == "Testing" || env.Contains("Test"))
            {
                // Use a test-friendly mock Key Vault manager
                services.AddSingleton<IKeyVaultSecretManager>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<TestKeyVaultSecretManager>>();
                    return new TestKeyVaultSecretManager(logger);
                });
            }
            else
            {
                throw new InvalidOperationException("KeyVault:Url must be configured in non-test environments");
            }
        }
        else
        {
            services.AddSingleton<IKeyVaultSecretManager>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<KeyVaultSecretManager>>();
                return new KeyVaultSecretManager(keyVaultUrl, logger);
            });
        }

        var jwtIssuer = configuration["JWT:Issuer"] ?? "https://deltagrid.io";
        var jwtAudience = configuration["JWT:Audience"] ?? "deltagrid-api";
        var jwtSigningKeyName = configuration["JWT:SigningKeyName"] ?? "jwt-signing-key";

        services.AddSingleton<IJwtValidator>(sp =>
        {
            var keyVault = sp.GetRequiredService<IKeyVaultSecretManager>();
            var logger = sp.GetRequiredService<ILogger<JwtValidator>>();
            return new JwtValidator(keyVault, jwtIssuer, jwtAudience, jwtSigningKeyName, logger);
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
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
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("SecurityExtensions");
                        logger.LogError(ex, "Failed to configure JWT signing key from Key Vault");
                        throw;
                    }
                });
        });

        return services;
    }

    /// <summary>
    /// Configure CORS with allowed origins
    /// </summary>
    public static IServiceCollection AddSecureCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:5001" };

        services.AddCors(options =>
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

        return services;
    }

    /// <summary>
    /// Configure request size limits
    /// </summary>
    public static IServiceCollection AddRequestSizeLimits(
        this IServiceCollection services,
        int maxSizeBytes = 10_485_760)
    {
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxSizeBytes;
            options.ValueLengthLimit = maxSizeBytes / 2;
            options.ValueCountLimit = 100;
        });

        return services;
    }

    /// <summary>
    /// Add request size limit middleware
    /// </summary>
    public static IApplicationBuilder UseRequestSizeLimit(
        this IApplicationBuilder app,
        int maxSizeBytes = 10_485_760)
    {
        app.Use(async (ctx, next) =>
        {
            ctx.Request.EnableBuffering();
            
            if (ctx.Request.ContentLength > maxSizeBytes)
            {
                ctx.Response.StatusCode = 413; // Payload Too Large
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync($"{{\"error\":\"Request payload exceeds maximum size of {maxSizeBytes / 1_048_576}MB\"}}");
                return;
            }
            
            await next();
        });

        return app;
    }
}

