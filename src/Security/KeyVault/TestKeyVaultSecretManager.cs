using Microsoft.Extensions.Logging;

namespace IOC.Security.KeyVault;

/// <summary>
/// Test-friendly Key Vault secret manager that uses in-memory secrets for testing
/// </summary>
public sealed class TestKeyVaultSecretManager : IKeyVaultSecretManager
{
    private readonly ILogger<TestKeyVaultSecretManager> _logger;
    private readonly Dictionary<string, string> _secrets;

    public TestKeyVaultSecretManager(ILogger<TestKeyVaultSecretManager> logger)
    {
        _logger = logger;
        _secrets = new Dictionary<string, string>
        {
            // Default test JWT signing key (base64 encoded 256-bit key)
            ["jwt-signing-key"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-jwt-signing-key-for-unit-tests-only-256-bits-long-key"))
        };
    }

    public Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        if (_secrets.TryGetValue(secretName, out var secret))
        {
            _logger.LogDebug("Retrieved test secret: {SecretName}", MaskSensitive(secretName));
            return Task.FromResult(secret);
        }

        // For tests, return a default secret rather than throwing
        _logger.LogWarning("Test secret not found: {SecretName}, using default", MaskSensitive(secretName));
        return Task.FromResult(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"test-default-secret-{secretName}")));
    }

    public Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        _secrets[secretName] = value;
        _logger.LogDebug("Set test secret: {SecretName}", MaskSensitive(secretName));
        return Task.CompletedTask;
    }

    public Task RotateSecretAsync(string secretName, string newValue, CancellationToken ct = default)
    {
        _secrets[secretName] = newValue;
        _logger.LogDebug("Rotated test secret: {SecretName}", MaskSensitive(secretName));
        return Task.CompletedTask;
    }

    public Task<List<SecretVersion>> GetSecretVersionsAsync(string secretName, CancellationToken ct = default)
    {
        // For tests, return a single version
        var now = DateTimeOffset.UtcNow;
        var versions = new List<SecretVersion>
        {
            new SecretVersion
            {
                VersionId = $"{secretName}/test-version-1",
                Enabled = true,
                CreatedOn = now,
                UpdatedOn = now
            }
        };
        return Task.FromResult(versions);
    }

    private static string MaskSensitive(string input)
    {
        if (string.IsNullOrEmpty(input)) return "***";
        if (input.Length <= 4) return "****";
        return input.Substring(0, 2) + "****" + input.Substring(input.Length - 2);
    }
}

