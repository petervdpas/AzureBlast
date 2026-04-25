using System;
using System.Threading;
using System.Threading.Tasks;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Resolver-aware overloads for the AzureBlast components, shaped for use with
/// any <c>Func&lt;category, key, ct, Task&lt;string&gt;&gt;</c> delegate (typically
/// <c>Secrets.Resolver</c> from TaskBlaster / SecretBlast). The library stays
/// free of any vault dependency: callers wire the delegate, components fetch
/// values via that delegate at <c>SetupAsync</c> / <c>InitializeAsync</c> time.
/// </summary>
/// <remarks>
/// <para>
/// Default key conventions (override per call when the vault uses different names):
/// </para>
/// <list type="bullet">
/// <item><see cref="IMssqlDatabase"/>: <c>(name, "connectionString")</c></item>
/// <item><see cref="IAzureServiceBus"/>: <c>(name, "connectionString")</c> + <c>(name, "queueName")</c></item>
/// <item><see cref="IAzureTableStorage"/>: <c>(name, "connectionString")</c> + optional <c>(name, "tableName")</c></item>
/// <item><see cref="IAzureKeyVault"/>: <c>(name, "url")</c></item>
/// </list>
/// </remarks>
public static class AzureBlastResolverExtensions
{
    /// <summary>
    /// Pulls the connection string from the supplied resolver and forwards to <see cref="IDatabase.Setup(string)"/>.
    /// </summary>
    /// <param name="database">The database to configure.</param>
    /// <param name="resolver">Secret resolver delegate.</param>
    /// <param name="connectionName">Logical connection name (the resolver category).</param>
    /// <param name="connectionStringKey">Key inside the category holding the connection string. Defaults to <c>connectionString</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SetupAsync(
        this IMssqlDatabase database,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string connectionStringKey = "connectionString",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connectionString = await resolver(connectionName, connectionStringKey, cancellationToken).ConfigureAwait(false);
        database.Setup(connectionString);
    }

    /// <summary>
    /// Pulls the connection string + queue name from the supplied resolver and forwards to <see cref="IAzureServiceBus.Setup(string?, string?, string)"/>.
    /// </summary>
    /// <param name="serviceBus">The Service Bus client to configure.</param>
    /// <param name="resolver">Secret resolver delegate.</param>
    /// <param name="connectionName">Logical connection name (the resolver category).</param>
    /// <param name="connectionStringKey">Key holding the Service Bus connection string. Defaults to <c>connectionString</c>.</param>
    /// <param name="queueNameKey">Key holding the queue name. Defaults to <c>queueName</c>.</param>
    /// <param name="contentType">Default outgoing message content type. Defaults to <c>application/json</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task SetupAsync(
        this IAzureServiceBus serviceBus,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string connectionStringKey = "connectionString",
        string queueNameKey = "queueName",
        string contentType = "application/json",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceBus);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connectionString = await resolver(connectionName, connectionStringKey, cancellationToken).ConfigureAwait(false);
        var queueName        = await resolver(connectionName, queueNameKey,        cancellationToken).ConfigureAwait(false);
        serviceBus.Setup(connectionString, queueName, contentType);
    }

    /// <summary>
    /// Pulls the connection string (and optional table name) from the supplied resolver and
    /// forwards to <see cref="IAzureTableStorage.Initialize(string, string?)"/>. Set <paramref name="tableNameKey"/>
    /// to <c>null</c> to skip the table-name lookup entirely.
    /// </summary>
    /// <param name="tableStorage">The table storage client to configure.</param>
    /// <param name="resolver">Secret resolver delegate.</param>
    /// <param name="connectionName">Logical connection name (the resolver category).</param>
    /// <param name="connectionStringKey">Key holding the storage connection string. Defaults to <c>connectionString</c>.</param>
    /// <param name="tableNameKey">Key holding the optional table name. Defaults to <c>tableName</c>; pass <c>null</c> to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task InitializeAsync(
        this IAzureTableStorage tableStorage,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string connectionStringKey = "connectionString",
        string? tableNameKey = "tableName",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tableStorage);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var connectionString = await resolver(connectionName, connectionStringKey, cancellationToken).ConfigureAwait(false);
        string? tableName = null;
        if (tableNameKey is not null)
        {
            try { tableName = await resolver(connectionName, tableNameKey, cancellationToken).ConfigureAwait(false); }
            catch { /* table name is optional; resolvers may throw on missing key */ }
            if (string.IsNullOrEmpty(tableName)) tableName = null;
        }
        tableStorage.Initialize(connectionString, tableName);
    }

    /// <summary>
    /// Pulls the vault URL from the supplied resolver and forwards to <see cref="IAzureKeyVault.InitializeKeyVaultAsync(string)"/>.
    /// </summary>
    /// <param name="keyVault">The Key Vault client to configure.</param>
    /// <param name="resolver">Secret resolver delegate.</param>
    /// <param name="connectionName">Logical connection name (the resolver category).</param>
    /// <param name="urlKey">Key holding the vault URL. Defaults to <c>url</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task InitializeKeyVaultAsync(
        this IAzureKeyVault keyVault,
        Func<string, string, CancellationToken, Task<string>> resolver,
        string connectionName,
        string urlKey = "url",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        var url = await resolver(connectionName, urlKey, cancellationToken).ConfigureAwait(false);
        await keyVault.InitializeKeyVaultAsync(url).ConfigureAwait(false);
    }
}
