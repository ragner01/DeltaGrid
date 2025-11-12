using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace IOC.Security.KeyVault;

/// <summary>
/// Azure Key Vault secret manager with managed identity support
/// </summary>
public interface IKeyVaultSecretManager
{
    /// <summary>
    /// Get a secret from Key Vault
    /// </summary>
    Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);

    /// <summary>
    /// Set a secret in Key Vault
    /// </summary>
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken ct = default);

    /// <summary>
    /// Rotate a secret (creates new version, marks old as deprecated)
    /// </summary>
    Task RotateSecretAsync(string secretName, string newValue, CancellationToken ct = default);

    /// <summary>
    /// Get all secret versions for a secret
    /// </summary>
    Task<List<SecretVersion>> GetSecretVersionsAsync(string secretName, CancellationToken ct = default);
}

/// <summary>
/// Azure Key Vault secret manager implementation
/// </summary>
public sealed class KeyVaultSecretManager : IKeyVaultSecretManager
{
    private readonly SecretClient _client;
    private readonly ILogger<KeyVaultSecretManager> _logger;

    public KeyVaultSecretManager(string keyVaultUrl, ILogger<KeyVaultSecretManager> logger)
    {
        var credential = new DefaultAzureCredential(); // Uses Managed Identity in Azure, Azure CLI in local dev
        _client = new SecretClient(new Uri(keyVaultUrl), credential);
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        try
        {
            var secret = await _client.GetSecretAsync(secretName, cancellationToken: ct);
            // Mask secret name in logs to prevent information disclosure
            var maskedName = MaskSensitive(secretName);
            _logger.LogInformation("Retrieved secret from Key Vault", new { SecretName = maskedName });
            return secret.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            var maskedName = MaskSensitive(secretName);
            _logger.LogWarning("Secret not found in Key Vault", new { SecretName = maskedName });
            throw new InvalidOperationException($"Secret {secretName} not found", ex);
        }
    }
    
    private static string MaskSensitive(string input)
    {
        if (string.IsNullOrEmpty(input)) return "***";
        if (input.Length <= 4) return "****";
        return input.Substring(0, 2) + "****" + input.Substring(input.Length - 2);
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken ct = default)
    {
        await _client.SetSecretAsync(secretName, secretValue, ct);
        var maskedName = MaskSensitive(secretName);
        _logger.LogInformation("Set secret in Key Vault", new { SecretName = maskedName });
    }

    public async Task RotateSecretAsync(string secretName, string newValue, CancellationToken ct = default)
    {
        // Set new version (creates new version automatically)
        await SetSecretAsync(secretName, newValue, ct);

        // Mark old versions as deprecated (optional - Key Vault doesn't delete old versions by default)
        var versions = await GetSecretVersionsAsync(secretName, ct);
        if (versions.Count > 1)
        {
            var maskedName = MaskSensitive(secretName);
            _logger.LogInformation("Secret rotated", new { SecretName = maskedName, VersionCount = versions.Count });
        }
    }

    public async Task<List<SecretVersion>> GetSecretVersionsAsync(string secretName, CancellationToken ct = default)
    {
        var versions = new List<SecretVersion>();
        await foreach (var version in _client.GetPropertiesOfSecretVersionsAsync(secretName, ct))
        {
            versions.Add(new SecretVersion
            {
                VersionId = version.Id.ToString(),
                Enabled = version.Enabled ?? true,
                CreatedOn = version.CreatedOn ?? DateTimeOffset.MinValue,
                UpdatedOn = version.UpdatedOn ?? DateTimeOffset.MinValue
            });
        }
        return versions;
    }
}

/// <summary>
/// Secret version metadata
/// </summary>
public sealed class SecretVersion
{
    public required string VersionId { get; init; }
    public bool Enabled { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset UpdatedOn { get; init; }
}

