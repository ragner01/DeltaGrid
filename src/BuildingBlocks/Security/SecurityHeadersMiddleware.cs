using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IOC.BuildingBlocks.Security;

/// <summary>
/// Middleware to add security headers to all HTTP responses
/// </summary>
public sealed class SecurityHeadersMiddleware
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
        // HSTS - Force HTTPS (only for HTTPS requests)
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

/// <summary>
/// Extension methods for SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

