using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast;

/// <summary>
///     Provides a static factory API for creating and configuring AzureBlast services.
///     Useful in non-DI contexts such as LINQPad, PowerShell, or standalone scripts,
///     while still supporting dependency injection when desired.
/// </summary>
public static class AzureBlastFactory
{
    /// <summary>
    ///     Builds a full <see cref="IServiceProvider" /> with AzureBlast services configured
    ///     from the given options.
    /// </summary>
    /// <param name="configure">
    ///     A delegate to configure the <see cref="AzureBlastOptions" /> used to initialize services.
    /// </param>
    /// <returns>
    ///     A fully built <see cref="IServiceProvider" /> containing all requested AzureBlast services.
    /// </returns>
    public static IServiceProvider CreateServiceProvider(Action<AzureBlastOptions> configure)
    {
        var services = CreateServiceCollection(configure);
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Creates an <see cref="IServiceCollection" /> with AzureBlast services registered,
    ///     allowing the caller to add their own services before building the provider.
    /// </summary>
    /// <param name="configure">
    ///     A delegate to configure the <see cref="AzureBlastOptions" /> used to initialize services.
    /// </param>
    /// <returns>
    ///     An <see cref="IServiceCollection" /> preconfigured with AzureBlast services.
    /// </returns>
    public static IServiceCollection CreateServiceCollection(Action<AzureBlastOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(configure);
        return services;
    }

    /// <summary>
    ///     Creates and configures an <see cref="IMssqlDatabase" /> instance for SQL Server access.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string to use.</param>
    /// <returns>A configured <see cref="IMssqlDatabase" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the connection string is null or empty.</exception>
    public static IMssqlDatabase CreateDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var db = new MssqlDatabase();
        db.Setup(connectionString);
        return db;
    }

    /// <summary>
    ///     Creates an <see cref="IAzureKeyVault" /> instance and initializes it synchronously.
    ///     Uses <see cref="DefaultAzureCredential" /> if no credential is provided.
    /// </summary>
    /// <param name="vaultUrl">The URL of the Azure Key Vault.</param>
    /// <param name="credential">Optional <see cref="TokenCredential" /> override. Defaults to <see cref="DefaultAzureCredential" />.</param>
    /// <returns>A fully initialized <see cref="IAzureKeyVault" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="vaultUrl"/> is null or empty.</exception>
    public static IAzureKeyVault CreateKeyVault(string vaultUrl, TokenCredential? credential = null)
    {
        if (string.IsNullOrWhiteSpace(vaultUrl))
            throw new ArgumentException("Vault URL cannot be null or empty.", nameof(vaultUrl));

        var cred = credential ?? new DefaultAzureCredential();
        var vault = new AzureKeyVault(cred);
        vault.InitializeKeyVaultAsync(vaultUrl).GetAwaiter().GetResult();
        return vault;
    }

    /// <summary>
    ///     Creates an <see cref="IAzureKeyVault" /> instance and initializes it asynchronously.
    ///     Uses <see cref="DefaultAzureCredential" /> if no credential is provided.
    /// </summary>
    /// <param name="vaultUrl">The URL of the Azure Key Vault.</param>
    /// <param name="credential">Optional <see cref="TokenCredential" /> override. Defaults to <see cref="DefaultAzureCredential" />.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, containing the initialized <see cref="IAzureKeyVault" /> instance.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="vaultUrl"/> is null or empty.</exception>
    public static async Task<IAzureKeyVault> CreateKeyVaultAsync(string vaultUrl, TokenCredential? credential = null)
    {
        if (string.IsNullOrWhiteSpace(vaultUrl))
            throw new ArgumentException("Vault URL cannot be null or empty.", nameof(vaultUrl));

        var cred = credential ?? new DefaultAzureCredential();
        var vault = new AzureKeyVault(cred);
        await vault.InitializeKeyVaultAsync(vaultUrl);
        return vault;
    }

    /// <summary>
    ///     Creates an <see cref="IAzureServiceBus" /> instance for sending and receiving messages.
    /// </summary>
    /// <param name="connectionString">The Service Bus connection string.</param>
    /// <param name="queueName">The queue name to bind to.</param>
    /// <returns>A configured <see cref="IAzureServiceBus" /> instance.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if <paramref name="connectionString"/> or <paramref name="queueName"/> is null or empty.
    /// </exception>
    public static IAzureServiceBus CreateServiceBus(string connectionString, string queueName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("SB connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name is required.", nameof(queueName));

        var sb = new AzureServiceBus();
        sb.Setup(connectionString, queueName);
        return sb;
    }

    /// <summary>
    ///     Creates an <see cref="IAzureTableStorage" /> instance for working with Azure Table Storage.
    /// </summary>
    /// <param name="connectionString">The Table Storage connection string.</param>
    /// <param name="tableName">Optional table name to configure immediately.</param>
    /// <returns>A configured <see cref="IAzureTableStorage" /> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
    public static IAzureTableStorage CreateTableStorage(string connectionString, string? tableName = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Table Storage connection string is required.", nameof(connectionString));

        var ts = new AzureTableStorage();
        ts.Initialize(connectionString, tableName);
        return ts;
    }

    /// <summary>
    ///     Creates an <see cref="IArmClientWrapper" /> instance that wraps an <see cref="Azure.ResourceManager.ArmClient" />.
    ///     Uses <see cref="DefaultAzureCredential" /> if no credential is provided.
    /// </summary>
    /// <param name="credential">Optional <see cref="TokenCredential" /> override. Defaults to <see cref="DefaultAzureCredential" />.</param>
    /// <returns>An <see cref="IArmClientWrapper" /> instance for interacting with Azure resources.</returns>
    public static IArmClientWrapper CreateArmClientWrapper(TokenCredential? credential = null)
    {
        var cred = credential ?? new DefaultAzureCredential();
        return new ArmClientWrapper(cred);
    }

    /// <summary>
    ///     Creates an <see cref="IAzureResourceClient" /> for querying Azure resources.
    ///     Uses <see cref="DefaultAzureCredential" /> if no credential is provided.
    /// </summary>
    /// <param name="credential">Optional <see cref="TokenCredential" /> override. Defaults to <see cref="DefaultAzureCredential" />.</param>
    /// <returns>A configured <see cref="IAzureResourceClient" /> instance.</returns>
    public static IAzureResourceClient CreateResourceClient(TokenCredential? credential = null)
    {
        var wrapper = CreateArmClientWrapper(credential);
        return new AzureResourceClient(wrapper);
    }
}
