using System;
using Azure.Core;
using Azure.Identity;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast;

/// <summary>
/// Fluent builder for composing AzureBlast services step-by-step and then registering them.
/// </summary>
public sealed class AzureBlastBuilder
{
    private readonly AzureBlastOptions _options = new();

    // These are populated when the builder is created via IServiceCollection extension.
    private readonly IServiceCollection? _servicesFromRegistration;
    private readonly TokenCredential? _credentialFromRegistration;

    /// <summary>
    /// Creates a standalone builder (no pre-attached service collection).
    /// Use <see cref="Build(Microsoft.Extensions.DependencyInjection.IServiceCollection?)"/> to apply registrations.
    /// </summary>
    public AzureBlastBuilder() { }

    /// <summary>
    /// Creates a builder bound to an existing <see cref="IServiceCollection"/> and an initial credential.
    /// This is the ctor used by <c>services.AddAzureBlast(...)</c>.
    /// </summary>
    /// <param name="services">The service collection to register into when <see cref="Build"/> is called.</param>
    /// <param name="credential">The default credential to use for Azure clients if none is later supplied.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="credential"/> is null.</exception>
    public AzureBlastBuilder(IServiceCollection services, TokenCredential credential)
    {
        _servicesFromRegistration = services ?? throw new ArgumentNullException(nameof(services));
        _credentialFromRegistration = credential ?? throw new ArgumentNullException(nameof(credential));
        _options.Credential = credential;
    }

    /// <summary>Sets a custom <see cref="TokenCredential"/> used by Azure clients.</summary>
    public AzureBlastBuilder WithCredential(TokenCredential credential)
    {
        _options.Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        return this;
    }

    /// <summary>Configures SQL Server access for <see cref="IMssqlDatabase"/>.</summary>
    public AzureBlastBuilder WithSql(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        _options.SqlConnectionString = connectionString;
        return this;
    }

    /// <summary>Configures Azure Key Vault (by URL) for <see cref="IAzureKeyVault"/>.</summary>
    public AzureBlastBuilder WithKeyVault(string vaultUrl)
    {
        if (string.IsNullOrWhiteSpace(vaultUrl))
            throw new ArgumentException("Vault URL cannot be null or empty.", nameof(vaultUrl));
        _options.KeyVaultUrl = vaultUrl;
        return this;
    }

    /// <summary>Configures Azure Table Storage for <see cref="IAzureTableStorage"/>.</summary>
    public AzureBlastBuilder WithTableStorage(string connectionString, string? tableName = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Table Storage connection string is required.", nameof(connectionString));
        _options.TableStorageConnectionString = connectionString;
        _options.TableName = tableName;
        return this;
    }

    /// <summary>Configures Azure Service Bus for <see cref="IAzureServiceBus"/>.</summary>
    public AzureBlastBuilder WithServiceBus(string connectionString, string queueName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Service Bus connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name is required.", nameof(queueName));
        _options.ServiceBusConnectionString = connectionString;
        _options.ServiceBusQueueName = queueName;
        return this;
    }

    /// <summary>
    /// Builds (or augments) an <see cref="IServiceCollection"/> with the configured services.
    /// If <paramref name="services"/> is null, uses the collection supplied via the ctor (if any), otherwise creates a new one.
    /// </summary>
    public IServiceCollection Build(IServiceCollection? services = null)
    {
        services ??= _servicesFromRegistration ?? new ServiceCollection();

        var credential = _options.Credential
                          ?? _credentialFromRegistration
                          ?? new DefaultAzureCredential();

        // SQL
        if (!string.IsNullOrWhiteSpace(_options.SqlConnectionString))
        {
            services.AddTransient<IMssqlDatabase>(_ =>
            {
                var db = new MssqlDatabase();
                db.Setup(_options.SqlConnectionString!);
                return db;
            });
        }

        // Key Vault (initialize using InitializeKeyVaultAsync — not SetVaultUri)
        if (!string.IsNullOrWhiteSpace(_options.KeyVaultUrl))
        {
            services.AddSingleton<IAzureKeyVault>(_ =>
            {
                var vault = new AzureKeyVault(credential);
                vault.InitializeKeyVaultAsync(_options.KeyVaultUrl!).GetAwaiter().GetResult();
                return vault;
            });
        }

        // ARM wrapper + resource client
        services.AddSingleton<IArmClientWrapper>(_ => new ArmClientWrapper(credential));
        services.AddSingleton<IAzureResourceClient>(sp =>
            new AzureResourceClient(sp.GetRequiredService<IArmClientWrapper>()));

        // Service Bus
        if (!string.IsNullOrWhiteSpace(_options.ServiceBusConnectionString) &&
            !string.IsNullOrWhiteSpace(_options.ServiceBusQueueName))
        {
            services.AddSingleton<IAzureServiceBus>(_ =>
            {
                var sb = new AzureServiceBus();
                sb.Setup(_options.ServiceBusConnectionString!, _options.ServiceBusQueueName!);
                return sb;
            });
        }

        // Table Storage
        if (!string.IsNullOrWhiteSpace(_options.TableStorageConnectionString))
        {
            services.AddSingleton<IAzureTableStorage>(_ =>
            {
                var ts = new AzureTableStorage();
                ts.Initialize(_options.TableStorageConnectionString!, _options.TableName);
                return ts;
            });
        }

        return services;
    }

    /// <summary>Builds a new <see cref="IServiceProvider"/> using the configured services.</summary>
    public IServiceProvider BuildServiceProvider()
        => Build().BuildServiceProvider();
}