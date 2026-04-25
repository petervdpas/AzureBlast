using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AzureBlast.Interfaces;
using Xunit;

namespace AzureBlast.Tests;

/// <summary>
/// Tests the resolver-aware extension methods on <see cref="AzureBlastResolverExtensions"/>:
/// each component must call the supplied resolver with the documented (name, key) pairs and
/// forward the resolved values to the existing string-based <c>Setup</c> / <c>Initialize</c>.
/// </summary>
public class AzureBlastResolverExtensionsTests
{
    private const string LocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;Pooling=false;";

    private static Func<string, string, CancellationToken, Task<string>> Recording(
        out List<(string category, string key)> calls,
        Func<string, string, string> respond)
    {
        var capture = new List<(string, string)>();
        calls = capture;
        return (category, key, _) =>
        {
            capture.Add((category, key));
            return Task.FromResult(respond(category, key));
        };
    }

    // ---------- IMssqlDatabase ----------

    [Fact]
    public async Task MssqlDatabase_SetupAsync_CallsResolverWithDefaultKey_AndForwardsConnectionString()
    {
        var resolver = Recording(out var calls, (_, _) => LocalDbConnectionString);
        var db = new MssqlDatabase();

        await db.SetupAsync(resolver, "azure-prod-sql");

        Assert.Single(calls);
        Assert.Equal(("azure-prod-sql", "connectionString"), calls[0]);
        Assert.Equal(LocalDbConnectionString, GetField<string>(db, "_connectionString"));
    }

    [Fact]
    public async Task MssqlDatabase_SetupAsync_HonoursCustomConnectionStringKey()
    {
        var resolver = Recording(out var calls, (_, _) => LocalDbConnectionString);
        var db = new MssqlDatabase();

        await db.SetupAsync(resolver, "azure-prod-sql", connectionStringKey: "dsn");

        Assert.Equal(("azure-prod-sql", "dsn"), calls[0]);
    }

    [Fact]
    public async Task MssqlDatabase_SetupAsync_NullArguments_Throw()
    {
        var resolver = Recording(out _, (_, _) => LocalDbConnectionString);
        var db = new MssqlDatabase();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IMssqlDatabase)null!).SetupAsync(resolver, "x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            db.SetupAsync(null!, "x"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            db.SetupAsync(resolver, ""));
    }

    // ---------- IAzureServiceBus ----------

    [Fact]
    public async Task AzureServiceBus_SetupAsync_CallsResolverWithBothDefaultKeys_AndForwardsValues()
    {
        var resolver = Recording(out var calls, (_, key) => key switch
        {
            "connectionString" => "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKey=k",
            "queueName"        => "orders",
            _                   => throw new InvalidOperationException(),
        });
        var sb = new AzureServiceBus();

        await sb.SetupAsync(resolver, "orders");

        Assert.Equal(2, calls.Count);
        Assert.Contains(("orders", "connectionString"), calls);
        Assert.Contains(("orders", "queueName"), calls);
        Assert.Equal("orders", GetField<string>(sb, "_queueName"));
    }

    [Fact]
    public async Task AzureServiceBus_SetupAsync_HonoursCustomKeysAndContentType()
    {
        var resolver = Recording(out var calls, (_, key) => key switch
        {
            "dsn"   => "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKey=k",
            "queue" => "events",
            _        => "",
        });
        var sb = new AzureServiceBus();

        await sb.SetupAsync(resolver, "events", connectionStringKey: "dsn", queueNameKey: "queue", contentType: "text/plain");

        Assert.Contains(("events", "dsn"), calls);
        Assert.Contains(("events", "queue"), calls);
    }

    [Fact]
    public async Task AzureServiceBus_SetupAsync_NullArguments_Throw()
    {
        var resolver = Recording(out _, (_, _) => "x");
        var sb = new AzureServiceBus();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IAzureServiceBus)null!).SetupAsync(resolver, "x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sb.SetupAsync(null!, "x"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sb.SetupAsync(resolver, ""));
    }

    // ---------- IAzureTableStorage ----------

    [Fact]
    public async Task AzureTableStorage_InitializeAsync_LooksUpConnectionStringAndOptionalTableName()
    {
        var resolver = Recording(out var calls, (_, key) => key switch
        {
            "connectionString" => "DefaultEndpointsProtocol=https;AccountName=t;AccountKey=k;EndpointSuffix=core.windows.net",
            "tableName"        => "events",
            _                   => "",
        });
        var ctor = typeof(AzureTableStorage).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(Func<string, TableServiceClient>), typeof(Func<string, string, TableClient>) })!;
        var table = (AzureTableStorage)ctor.Invoke(new object[]
        {
            (Func<string, TableServiceClient>)(_ => new TableServiceClient("UseDevelopmentStorage=true")),
            (Func<string, string, TableClient>)((_, _) => new TableClient("UseDevelopmentStorage=true", "x")),
        });

        await table.InitializeAsync(resolver, "events");

        Assert.Contains(("events", "connectionString"), calls);
        Assert.Contains(("events", "tableName"), calls);
    }

    [Fact]
    public async Task AzureTableStorage_InitializeAsync_NullTableNameKey_SkipsTableLookup()
    {
        var resolver = Recording(out var calls, (_, _) =>
            "DefaultEndpointsProtocol=https;AccountName=t;AccountKey=k;EndpointSuffix=core.windows.net");
        var ctor = typeof(AzureTableStorage).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(Func<string, TableServiceClient>), typeof(Func<string, string, TableClient>) })!;
        var table = (AzureTableStorage)ctor.Invoke(new object[]
        {
            (Func<string, TableServiceClient>)(_ => new TableServiceClient("UseDevelopmentStorage=true")),
            (Func<string, string, TableClient>)((_, _) => new TableClient("UseDevelopmentStorage=true", "x")),
        });

        await table.InitializeAsync(resolver, "events", tableNameKey: null);

        Assert.Single(calls);
        Assert.Equal(("events", "connectionString"), calls[0]);
    }

    // ---------- IAzureKeyVault ----------

    [Fact]
    public async Task AzureKeyVault_InitializeKeyVaultAsync_CallsResolverWithDefaultUrlKey()
    {
        var resolver = Recording(out var calls, (_, _) => "https://contoso.vault.azure.net/");
        var kv = new AzureKeyVault(new DefaultAzureCredential(),
            (uri, cred) => new SecretClient(uri, cred));

        await kv.InitializeKeyVaultAsync(resolver, "kv-prod");

        Assert.Single(calls);
        Assert.Equal(("kv-prod", "url"), calls[0]);
    }

    [Fact]
    public async Task AzureKeyVault_InitializeKeyVaultAsync_HonoursCustomUrlKey()
    {
        var resolver = Recording(out var calls, (_, _) => "https://contoso.vault.azure.net/");
        var kv = new AzureKeyVault(new DefaultAzureCredential(),
            (uri, cred) => new SecretClient(uri, cred));

        await kv.InitializeKeyVaultAsync(resolver, "kv-prod", urlKey: "endpoint");

        Assert.Equal(("kv-prod", "endpoint"), calls[0]);
    }

    [Fact]
    public async Task AzureKeyVault_InitializeKeyVaultAsync_NullArguments_Throw()
    {
        var resolver = Recording(out _, (_, _) => "https://x.vault.azure.net/");
        var kv = new AzureKeyVault(new DefaultAzureCredential(),
            (uri, cred) => new SecretClient(uri, cred));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            ((IAzureKeyVault)null!).InitializeKeyVaultAsync(resolver, "x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            kv.InitializeKeyVaultAsync(null!, "x"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            kv.InitializeKeyVaultAsync(resolver, ""));
    }

    private static T GetField<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Missing field: {name}");
        return (T)field.GetValue(instance)!;
    }
}
