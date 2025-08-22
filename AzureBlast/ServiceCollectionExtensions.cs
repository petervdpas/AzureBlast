using System;
using Azure.Core;
using Azure.Identity;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast;

/// <summary>
///     Provides extension methods for registering AzureBlast services with a dependency injection container.
///     All components are registered optionally based on the provided configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers one or more AzureBlast services optionally via the <see cref="AzureBlastOptions" /> config.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configure">A delegate to configure <see cref="AzureBlastOptions" />.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAzureBlast(this IServiceCollection services, Action<AzureBlastOptions> configure)
    {
        var options = new AzureBlastOptions();
        configure(options);

        var credential = options.Credential ?? new DefaultAzureCredential();

        // Optional: SQL Database
        if (!string.IsNullOrWhiteSpace(options.SqlConnectionString))
        {
            services.AddTransient<IMssqlDatabase>(sp =>
            {
                var db = new MssqlDatabase();
                db.Setup(options.SqlConnectionString!);
                return db;
            });
        }

        // Optional: Azure Key Vault
        if (!string.IsNullOrWhiteSpace(options.KeyVaultUrl))
        {
            services.AddSingleton<IAzureKeyVault>(sp =>
            {
                var vault = new AzureKeyVault(credential);
                // sync-init is ok here since you're already doing it
                vault.InitializeKeyVaultAsync(options.KeyVaultUrl!).GetAwaiter().GetResult();
                return vault;
            });
        }

        // Optional: Azure ARM client (needs wrapper first)
        services.AddSingleton<IArmClientWrapper>(new ArmClientWrapper(credential));
        services.AddSingleton<IAzureResourceClient>(sp =>
        {
            var wrapper = sp.GetRequiredService<IArmClientWrapper>();
            return new AzureResourceClient(wrapper);
        });

        // Optional: Azure Service Bus
        if (!string.IsNullOrWhiteSpace(options.ServiceBusConnectionString) &&
            !string.IsNullOrWhiteSpace(options.ServiceBusQueueName))
        {
            services.AddSingleton<IAzureServiceBus>(sp =>
            {
                var sb = new AzureServiceBus();
                sb.Setup(options.ServiceBusConnectionString, options.ServiceBusQueueName);
                return sb;
            });
        }

        // Optional: Azure Table Storage
        if (!string.IsNullOrWhiteSpace(options.TableStorageConnectionString))
        {
            services.AddSingleton<IAzureTableStorage>(sp =>
            {
                var table = new AzureTableStorage();
                table.Initialize(options.TableStorageConnectionString, options.TableName);
                return table;
            });
        }

        return services;
    }
}