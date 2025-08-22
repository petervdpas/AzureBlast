using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using AzureBlast.Interfaces;
using Serilog;

namespace AzureBlast;

/// <summary>
///     Provides access to Azure Key Vault for managing secrets.
///     Implements <see cref="IAzureKeyVault" /> using <see cref="SecretClient" /> internally.
/// </summary>
public sealed class AzureKeyVault : IAzureKeyVault
{
    private readonly TokenCredential _credential;
    private readonly Func<Uri, TokenCredential, SecretClient> _clientFactory;
    private SecretClient? _secretClient;

    /// <summary>
    /// Create an AzureKeyVault using the provided credential.
    /// </summary>
    public AzureKeyVault(TokenCredential credential,
        Func<Uri, TokenCredential, SecretClient>? clientFactory = null)
    {
        _credential = credential;
        _clientFactory = clientFactory ?? ((u, c) => new SecretClient(u, c));
    }

    /// <inheritdoc />
    public Task InitializeKeyVaultAsync(string vaultUrl)
    {
        try
        {
            _secretClient = _clientFactory(new Uri(vaultUrl), _credential);
            Log.Debug("Key Vault client initialized for {VaultUrl}", vaultUrl);
        }
        catch (Exception ex)
        {
            Log.Error("Error initializing Key Vault client: {Message}", ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<List<string>> ListSecretsAsync()
    {
        EnsureClientInitialized(nameof(ListSecretsAsync));
        var result = new List<string>();

        try
        {
            await foreach (var secret in _secretClient!.GetPropertiesOfSecretsAsync())
                result.Add(secret.Name);

            Log.Debug($"Retrieved {result.Count} secrets.");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error($"Error listing secrets: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GetSecretAsync(string name)
    {
        EnsureClientInitialized(nameof(GetSecretAsync));

        try
        {
            var result = await _secretClient!.GetSecretAsync(name);
            return result.Value.Value;
        }
        catch (Exception ex)
        {
            Log.Error($"Error getting secret {name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetSecretAsync(string name, string value)
    {
        EnsureClientInitialized(nameof(SetSecretAsync));

        try
        {
            await _secretClient!.SetSecretAsync(name, value);
            Log.Debug($"Set secret {name}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Error setting secret {name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSecretAsync(string name)
    {
        EnsureClientInitialized(nameof(DeleteSecretAsync));

        try
        {
            var op = await _secretClient!.StartDeleteSecretAsync(name);
            await op.WaitForCompletionAsync();
            Log.Debug($"Deleted secret {name}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Error deleting secret {name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PurgeSecretAsync(string name)
    {
        EnsureClientInitialized(nameof(PurgeSecretAsync));

        try
        {
            await _secretClient!.PurgeDeletedSecretAsync(name);
            Log.Debug($"Purged secret {name}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Error purging secret {name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecoverDeletedSecretAsync(string name)
    {
        EnsureClientInitialized(nameof(RecoverDeletedSecretAsync));

        try
        {
            await _secretClient!.StartRecoverDeletedSecretAsync(name);
            Log.Debug($"Recovered secret {name}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Error recovering secret {name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Ensures the <see cref="SecretClient" /> is initialized before allowing method calls.
    /// </summary>
    /// <param name="caller">The name of the calling method, used in the exception message.</param>
    /// <exception cref="InvalidOperationException">Thrown when the client is not initialized.</exception>
    private void EnsureClientInitialized(string caller)
    {
        if (_secretClient == null)
            throw new InvalidOperationException(
                $"SecretClient not initialized. Call InitializeKeyVaultAsync before calling {caller}.");
    }
}