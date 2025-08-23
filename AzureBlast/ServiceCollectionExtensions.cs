using System;
using Azure.Identity;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions; // for TryAdd*

namespace AzureBlast;

/// <summary>
/// Extension methods for registering AzureBlast services with a dependency injection container.
/// </summary>
/// <remarks>
/// This registration is idempotent: it uses <c>TryAdd*</c> so your own registrations (added
/// earlier in the pipeline) won't be overwritten or duplicated.
/// <para/>
/// Only components you configure in <see cref="AzureBlastOptions"/> are registered. Unset or empty
/// options are ignored.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers one or more AzureBlast services based on the supplied <see cref="AzureBlastOptions"/>.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/> to populate.</param>
    /// <param name="configure">Delegate that populates an <see cref="AzureBlastOptions"/> instance.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddAzureBlast(o =>
    /// {
    ///     o.SqlConnectionString = "...";
    ///     o.KeyVaultUrl = "https://contoso.vault.azure.net/";
    ///     o.TableStorageConnectionString = "...";
    ///     o.TableName = "MyTable";
    ///     o.ServiceBusConnectionString = "...";
    ///     o.ServiceBusQueueName = "orders";
    ///     // Optional credential override; otherwise DefaultAzureCredential is used:
    ///     // o.Credential = new DefaultAzureCredential();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddAzureBlast(this IServiceCollection services, Action<AzureBlastOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AzureBlastOptions();
        configure(options);

        // Choose credential: provided via options or DefaultAzureCredential.
        var credential = options.Credential ?? new DefaultAzureCredential();

        // Optional: SQL Database
        if (!string.IsNullOrWhiteSpace(options.SqlConnectionString))
        {
            services.TryAddTransient<IMssqlDatabase>(_ =>
            {
                var db = new MssqlDatabase();
                db.Setup(options.SqlConnectionString!);
                return db;
            });
        }

        // Optional: Azure Key Vault
        if (!string.IsNullOrWhiteSpace(options.KeyVaultUrl))
        {
            services.TryAddSingleton<IAzureKeyVault>(_ =>
            {
                var vault = new AzureKeyVault(credential);
                // Synchronous init here is acceptable when composing the container at startup.
                vault.InitializeKeyVaultAsync(options.KeyVaultUrl!).GetAwaiter().GetResult();
                return vault;
            });
        }

        // Optional: Azure ARM client (wrapper) and resource client
        services.TryAddSingleton<IArmClientWrapper>(_ => new ArmClientWrapper(credential));
        services.TryAddSingleton<IAzureResourceClient>(sp =>
        {
            var wrapper = sp.GetRequiredService<IArmClientWrapper>();
            return new AzureResourceClient(wrapper);
        });

        // Optional: Azure Service Bus
        if (!string.IsNullOrWhiteSpace(options.ServiceBusConnectionString) &&
            !string.IsNullOrWhiteSpace(options.ServiceBusQueueName))
        {
            services.TryAddSingleton<IAzureServiceBus>(_ =>
            {
                var sb = new AzureServiceBus();
                sb.Setup(options.ServiceBusConnectionString!, options.ServiceBusQueueName!);
                return sb;
            });
        }

        // Optional: Azure Table Storage
        if (!string.IsNullOrWhiteSpace(options.TableStorageConnectionString))
        {
            services.TryAddSingleton<IAzureTableStorage>(_ =>
            {
                var table = new AzureTableStorage();
                table.Initialize(options.TableStorageConnectionString!, options.TableName);
                return table;
            });
        }

        return services;
    }
}
