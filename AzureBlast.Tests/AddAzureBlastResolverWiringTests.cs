using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AzureBlast.Tests;

/// <summary>
/// Verifies AddAzureBlast picks the resolver path when a <c>*ConnectionName</c>
/// is configured, and falls back to the existing string-based path otherwise.
/// </summary>
public class AddAzureBlastResolverWiringTests
{
    private const string SqlConn =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=True;Pooling=false;";

    private static Func<string, string, CancellationToken, Task<string>> RecordingResolver(
        out List<(string category, string key)> calls,
        Func<string, string, string> respond)
    {
        var capture = new List<(string, string)>();
        calls = capture;
        return (c, k, _) => { capture.Add((c, k)); return Task.FromResult(respond(c, k)); };
    }

    [Fact]
    public void Sql_ResolverPath_TakesPrecedenceOverStringPath()
    {
        var resolver = RecordingResolver(out var calls, (_, _) => SqlConn);

        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.SqlConnectionString = "ignored=because-resolver-wins";
            o.Resolver            = resolver;
            o.SqlConnectionName   = "azure-prod-sql";
        });

        using var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<IMssqlDatabase>();

        Assert.NotNull(db);
        Assert.Contains(("azure-prod-sql", "connectionString"), calls);
    }

    [Fact]
    public void Sql_StringPath_StillWorks_WhenResolverNotSet()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(o => { o.SqlConnectionString = SqlConn; });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IMssqlDatabase>());
    }

    [Fact]
    public void Sql_NeitherStringNorResolverName_ComponentNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddAzureBlast(o => { /* nothing for SQL */ });

        using var sp = services.BuildServiceProvider();
        Assert.Null(sp.GetService<IMssqlDatabase>());
    }

    [Fact]
    public void ServiceBus_ResolverPath_RegistersComponent_WhenConnectionNameSet()
    {
        var resolver = RecordingResolver(out var calls, (_, key) => key switch
        {
            "connectionString" => "Endpoint=sb://x.servicebus.windows.net/;SharedAccessKey=k",
            "queueName"        => "orders",
            _                   => "",
        });

        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.Resolver                 = resolver;
            o.ServiceBusConnectionName = "orders";
        });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IAzureServiceBus>());
        Assert.Contains(("orders", "connectionString"), calls);
        Assert.Contains(("orders", "queueName"), calls);
    }

    [Fact]
    public void MixAndMatch_SqlViaResolver_KeyVaultViaString_BothRegistered()
    {
        var resolver = RecordingResolver(out var calls, (_, _) => SqlConn);

        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.Resolver           = resolver;
            o.SqlConnectionName  = "prod-sql";
            o.KeyVaultUrl        = "https://contoso.vault.azure.net/";
        });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IMssqlDatabase>());
        Assert.NotNull(sp.GetRequiredService<IAzureKeyVault>());

        Assert.Equal(("prod-sql", "connectionString"), calls.Single());
    }

    [Fact]
    public void Resolver_WithoutConnectionNames_BehavesLikeNoResolver()
    {
        // Resolver alone (no *ConnectionName fields) should still allow the string-based path.
        var resolver = RecordingResolver(out var calls, (_, _) => "unused");

        var services = new ServiceCollection();
        services.AddAzureBlast(o =>
        {
            o.Resolver            = resolver;
            o.SqlConnectionString = SqlConn;
        });

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IMssqlDatabase>());
        Assert.Empty(calls); // resolver never invoked
    }
}
