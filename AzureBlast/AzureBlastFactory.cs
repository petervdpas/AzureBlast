using System;
using Azure.Identity;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast;

/// <summary>
///     Provides a simple static API for manual creation of services, for non-DI contexts like LINQPad or PowerShell.
/// </summary>
public static class AzureBlastFactory
{
    /// <summary>
    ///     Creates a full <see cref="IServiceProvider" /> with AzureBlast services configured from the given options.
    /// </summary>
    /// <param name="configure">Delegate to populate <see cref="AzureBlastOptions" />.</param>
    /// <returns>A fully built <see cref="IServiceProvider" /> containing all requested services.</returns>
    public static IServiceProvider CreateServiceProvider(Action<AzureBlastOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(configure);
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Quickly creates an <see cref="IMssqlDatabase" /> instance manually.
    /// </summary>
    /// <param name="connectionString">SQL Server connection string.</param>
    /// <returns>Configured <see cref="IMssqlDatabase" />.</returns>
    public static IMssqlDatabase CreateDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var db = new MssqlDatabase();
        db.Setup(connectionString);
        return db;
    }

    /// <summary>
    ///     Creates an <see cref="IAzureKeyVault" /> instance using <see cref="DefaultAzureCredential" />.
    /// </summary>
    /// <param name="vaultUrl">The URL of the Azure Key Vault.</param>
    /// <returns>Initialized <see cref="IAzureKeyVault" /> instance.</returns>
    public static IAzureKeyVault CreateKeyVault(string vaultUrl)
    {
        if (string.IsNullOrWhiteSpace(vaultUrl))
            throw new ArgumentException("Vault URL cannot be null or empty.", nameof(vaultUrl));

        var credential = new DefaultAzureCredential();
        var vault = new AzureKeyVault(credential);
        vault.InitializeKeyVaultAsync(vaultUrl).GetAwaiter().GetResult();
        return vault;
    }
}