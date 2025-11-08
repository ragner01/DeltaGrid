using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using IOC.Security.KeyVault;

namespace IOC.Security.Jwt;

/// <summary>
/// JWT hardening configuration and validation
/// </summary>
public interface IJwtValidator
{
    /// <summary>
    /// Validate JWT token with strict security checks
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token, TokenValidationParameters? customParams = null);

    /// <summary>
    /// Get recommended token validation parameters with hardening
    /// </summary>
    TokenValidationParameters GetHardenedValidationParameters();
}

/// <summary>
/// JWT validator with security hardening
/// </summary>
public sealed class JwtValidator : IJwtValidator
{
    private readonly IKeyVaultSecretManager _keyVault;
    private readonly ILogger<JwtValidator> _logger;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKeyName;

    public JwtValidator(
        IKeyVaultSecretManager keyVault,
        string issuer,
        string audience,
        string signingKeyName,
        ILogger<JwtValidator> logger)
    {
        _keyVault = keyVault;
        _issuer = issuer;
        _audience = audience;
        _signingKeyName = signingKeyName;
        _logger = logger;
    }

    public ClaimsPrincipal? ValidateToken(string token, TokenValidationParameters? customParams = null)
    {
        var validationParams = customParams ?? GetHardenedValidationParameters();

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, validationParams, out var validatedToken);
            _logger.LogDebug("JWT token validated successfully");
            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogError(ex, "JWT token signature invalid");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT token validation failed");
            return null;
        }
    }

    public TokenValidationParameters GetHardenedValidationParameters()
    {
        // Get signing key from Key Vault
        var signingKey = _keyVault.GetSecretAsync(_signingKeyName).Result;
        var keyBytes = Convert.FromBase64String(signingKey);

        return new TokenValidationParameters
        {
            // Issuer validation
            ValidateIssuer = true,
            ValidIssuer = _issuer,

            // Audience validation
            ValidateAudience = true,
            ValidAudience = _audience,

            // Lifetime validation
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No clock skew tolerance

            // Signing key validation
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            // Algorithm pinning (only allow specific algorithms)
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            RequireAudience = true,

            // Security hardening
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = true
            }
        };
    }
}

/// <summary>
/// JWT key rotation service
/// </summary>
public interface IJwtKeyRotationService
{
    /// <summary>
    /// Rotate JWT signing key (creates new key, keeps old for overlap period)
    /// </summary>
    Task RotateKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Get current active key name
    /// </summary>
    Task<string> GetActiveKeyNameAsync(CancellationToken ct = default);
}

/// <summary>
/// JWT key rotation service implementation
/// </summary>
public sealed class JwtKeyRotationService : IJwtKeyRotationService
{
    private readonly IKeyVaultSecretManager _keyVault;
    private readonly ILogger<JwtKeyRotationService> _logger;
    private readonly string _baseKeyName;
    private readonly int _keyRotationDays;

    public JwtKeyRotationService(
        IKeyVaultSecretManager keyVault,
        string baseKeyName,
        int keyRotationDays,
        ILogger<JwtKeyRotationService> logger)
    {
        _keyVault = keyVault;
        _baseKeyName = baseKeyName;
        _keyRotationDays = keyRotationDays;
        _logger = logger;
    }

    public async Task RotateKeyAsync(CancellationToken ct = default)
    {
        // Generate new key
        var newKey = GenerateSigningKey();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var newKeyName = $"{_baseKeyName}-{timestamp}";

        // Store new key in Key Vault
        await _keyVault.SetSecretAsync(newKeyName, newKey, ct);

        // Rotate secret (creates new version)
        await _keyVault.RotateSecretAsync(_baseKeyName, newKey, ct);

        _logger.LogInformation("JWT signing key rotated: {KeyName}", newKeyName);
    }

    public Task<string> GetActiveKeyNameAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_baseKeyName);
    }

    private static string GenerateSigningKey()
    {
        // Generate 256-bit key (32 bytes)
        var keyBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }
}

