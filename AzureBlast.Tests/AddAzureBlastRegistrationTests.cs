using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast.Tests;

public class AddAzureBlastRegistrationTests
{
    // ---------- helpers ----------
    private static int CountDescriptors<T>(IServiceCollection sc)
        => sc.Count(d => d.ServiceType == typeof(T));

    // ---------- argument guards ----------
    [Fact]
    public void AddAzureBlast_Throws_On_Null_Services()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddAzureBlast(_ => { }));
    }

    [Fact]
    public void AddAzureBlast_Throws_On_Null_Configure()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddAzureBlast(null!));
    }

    // ---------- always-on registrations ----------
    [Fact]
    public void Registers_ArmWrapper_And_ResourceClient_Always()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(_ => { });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<IArmClientWrapper>());
        Assert.NotNull(sp.GetService<IAzureResourceClient>());
    }

    // ---------- gated registrations ----------
    [Fact]
    public void Registers_Sql_When_ConnectionString_Present_Transient()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.SqlConnectionString = "Server=.;Database=foo;Trusted_Connection=True;";
        });

        using var sp = services.BuildServiceProvider();
        var db1 = sp.GetRequiredService<IMssqlDatabase>();
        var db2 = sp.GetRequiredService<IMssqlDatabase>();

        Assert.NotNull(db1);
        Assert.NotNull(db2);
        Assert.NotSame(db1, db2); // transient
    }

    [Fact]
    public void Registers_KeyVault_When_Url_Present()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.KeyVaultUrl = "https://example.vault.azure.net/";
        });

        using var sp = services.BuildServiceProvider();
        var kv = sp.GetRequiredService<IAzureKeyVault>();
        Assert.NotNull(kv);
        Assert.IsType<AzureKeyVault>(kv);
    }

    [Fact]
    public void Registers_ServiceBus_Only_When_Conn_And_Queue_Present()
    {
        // Missing queue -> no registration
        var s1 = new ServiceCollection();
        s1.AddAzureBlast(o => o.ServiceBusConnectionString = "Endpoint=sb://x/;...");
        Assert.Equal(0, CountDescriptors<IAzureServiceBus>(s1));

        // Both present -> registrations exist
        var s2 = new ServiceCollection();
        s2.AddAzureBlast(o =>
        {
            o.ServiceBusConnectionString = "Endpoint=sb://x/;...";
            o.ServiceBusQueueName = "orders";
        });
        Assert.Equal(1, CountDescriptors<IAzureServiceBus>(s2));
    }

    [Fact]
    public void Registers_TableStorage_When_Conn_Present()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.TableStorageConnectionString = "UseDevelopmentStorage=true;";
            o.TableName = "MyTable";
        });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<IAzureTableStorage>());
    }

    // ---------- TryAdd idempotency / preserve existing ----------
    [Fact]
    public void TryAdd_DoesNot_Overwrite_Existing_Registration()
    {
        var services = new ServiceCollection();

        var existing = new FakeTableStorage();
        services.AddSingleton<IAzureTableStorage>(existing);

        services.AddAzureBlast(o =>
        {
            o.TableStorageConnectionString = "UseDevelopmentStorage=true;";
            o.TableName = "ShouldBeIgnoredBecauseAlreadyRegistered";
        });

        using var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<IAzureTableStorage>();

        Assert.Same(existing, resolved);
        Assert.Equal(1, CountDescriptors<IAzureTableStorage>(services));
    }

    [Fact]
    public void AddAzureBlast_Called_Twice_Does_Not_Duplicate()
    {
        var services = new ServiceCollection();

        services.AddAzureBlast(o =>
        {
            o.TableStorageConnectionString = "UseDevelopmentStorage=true;";
            o.ServiceBusConnectionString = "Endpoint=sb://x/;...";
            o.ServiceBusQueueName = "orders";
        });

        services.AddAzureBlast(o =>
        {
            // Second call with different values should not add duplicates due to TryAdd*
            o.TableStorageConnectionString = "UseDevelopmentStorage=true;ignored";
            o.ServiceBusConnectionString = "Endpoint=sb://x/;...ignored";
            o.ServiceBusQueueName = "orders2";
        });

        Assert.Equal(1, CountDescriptors<IAzureTableStorage>(services));
        Assert.Equal(1, CountDescriptors<IAzureServiceBus>(services));
        Assert.Equal(1, CountDescriptors<IArmClientWrapper>(services));
        Assert.Equal(1, CountDescriptors<IAzureResourceClient>(services));
    }

    private sealed class FakeTableStorage : IAzureTableStorage
    {
        public string? LastConnectionString { get; private set; }
        public string? LastInitTable { get; private set; }
        public string? CurrentTable { get; private set; }

        public void Initialize(string connectionString, string? tableName)
        {
            LastConnectionString = connectionString;
            LastInitTable = tableName;
            CurrentTable = tableName;
        }

        public Task<List<string>> ListTablesAsync() =>
            Task.FromResult(new List<string>());

        public void SetTable(string tableName) =>
            CurrentTable = tableName;

        public Task UpsertEntityAsync<T>(T entity) where T : ITableEntity =>
            Task.CompletedTask;

        public Task DeleteEntityAsync(string partitionKey, string rowKey) =>
            Task.CompletedTask;

        public Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey)
            where T : class, ITableEntity, new() =>
            Task.FromResult<T?>(null);

        public Task<List<T>> QueryEntitiesAsync<T>(string filter)
            where T : class, ITableEntity, new() =>
            Task.FromResult(new List<T>());

        public Task<List<string>> CheckEntitiesExistAsync<T>(List<string> rowKeys)
            where T : class, ITableEntity, new() =>
            Task.FromResult(new List<string>());
    }
}