using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace IOC.Security.Middleware;

/// <summary>
/// Middleware to add security headers (HSTS, CSP, X-Frame-Options, etc.)
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        SecurityHeadersOptions options,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS (HTTP Strict Transport Security)
        if (_options.EnableHsts)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                $"max-age={_options.HstsMaxAge}; includeSubDomains; preload");
        }

        // X-Frame-Options (clickjacking protection)
        context.Response.Headers.Append("X-Frame-Options", _options.FrameOptions);

        // X-Content-Type-Options (MIME sniffing protection)
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-XSS-Protection (legacy, but still useful)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Content Security Policy
        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            context.Response.Headers.Append("Content-Security-Policy", _options.ContentSecurityPolicy);
        }

        // Referrer-Policy
        context.Response.Headers.Append("Referrer-Policy", _options.ReferrerPolicy);

        // Permissions-Policy (formerly Feature-Policy)
        if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            context.Response.Headers.Append("Permissions-Policy", _options.PermissionsPolicy);
        }

        // CORS headers (if enabled)
        if (_options.EnableCors)
        {
            if (!string.IsNullOrEmpty(_options.AllowedOrigins))
            {
                context.Response.Headers.Append("Access-Control-Allow-Origin", _options.AllowedOrigins);
            }
            if (!string.IsNullOrEmpty(_options.AllowedMethods))
            {
                context.Response.Headers.Append("Access-Control-Allow-Methods", _options.AllowedMethods);
            }
            if (!string.IsNullOrEmpty(_options.AllowedHeaders))
            {
                context.Response.Headers.Append("Access-Control-Allow-Headers", _options.AllowedHeaders);
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Security headers configuration options
/// </summary>
public sealed class SecurityHeadersOptions
{
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year
    public string FrameOptions { get; set; } = "DENY"; // DENY, SAMEORIGIN, ALLOW-FROM
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https:; frame-ancestors 'none';";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";
    public bool EnableCors { get; set; } = false;
    public string AllowedOrigins { get; set; } = string.Empty;
    public string AllowedMethods { get; set; } = "GET, POST, PUT, DELETE, OPTIONS";
    public string AllowedHeaders { get; set; } = "Content-Type, Authorization, X-Requested-With";
}

/// <summary>
/// Extension methods for adding security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}

