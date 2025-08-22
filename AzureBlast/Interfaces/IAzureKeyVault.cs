using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureBlast.Interfaces;

/// <summary>
///     Provides an interface for interacting with Azure Key Vault to manage secrets.
/// </summary>
public interface IAzureKeyVault
{
    /// <summary>
    ///     Initializes the Azure Key Vault connection.
    /// </summary>
    /// <param name="vaultUrl">The URL of the Azure Key Vault.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task InitializeKeyVaultAsync(string vaultUrl);

    /// <summary>
    ///     Lists all secrets in the Azure Key Vault.
    /// </summary>
    /// <returns>A list of secret names.</returns>
    Task<List<string>> ListSecretsAsync();

    /// <summary>
    ///     Retrieves a secret from Azure Key Vault by its name.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the secret value as a
    ///     string.
    /// </returns>
    Task<string> GetSecretAsync(string secretName);

    /// <summary>
    ///     Sets or updates a secret in Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to set or update.</param>
    /// <param name="secretValue">The value of the secret.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task SetSecretAsync(string secretName, string secretValue);

    /// <summary>
    ///     Deletes a secret from Azure Key Vault (soft delete).
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task DeleteSecretAsync(string secretName);

    /// <summary>
    ///     Permanently deletes a secret from Azure Key Vault (purges a soft-deleted secret).
    /// </summary>
    /// <param name="secretName">The name of the secret to purge.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task PurgeSecretAsync(string secretName);

    /// <summary>
    ///     Recovers a previously deleted secret from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to recover.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    Task RecoverDeletedSecretAsync(string secretName);
}