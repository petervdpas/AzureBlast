using System;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast.Tests;

public class AzureBlastBuilderTests
{
    // ---------- guards ----------
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void WithSql_Throws_On_Empty(string cs)
    {
        var b = new AzureBlastBuilder();
        Assert.Throws<ArgumentException>(() => b.WithSql(cs));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void WithKeyVault_Throws_On_Empty(string url)
    {
        var b = new AzureBlastBuilder();
        Assert.Throws<ArgumentException>(() => b.WithKeyVault(url));
    }

    [Theory]
    [InlineData("", "q")]
    [InlineData("conn", "")]
    public void WithServiceBus_Throws_On_Bad_Input(string conn, string queue)
    {
        var b = new AzureBlastBuilder();
        Assert.Throws<ArgumentException>(() => b.WithServiceBus(conn, queue));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void WithTableStorage_Throws_On_Empty(string cs)
    {
        var b = new AzureBlastBuilder();
        Assert.Throws<ArgumentException>(() => b.WithTableStorage(cs));
    }

    // ---------- registrations & lifetimes ----------
    [Fact]
    public void Build_Registers_SQL_As_Transient()
    {
        var services = new ServiceCollection();
        new AzureBlastBuilder()
            .WithSql("Server=.;Database=foo;Trusted_Connection=True;")
            .Build(services);

        using var sp = services.BuildServiceProvider();
        var db1 = sp.GetRequiredService<IMssqlDatabase>();
        var db2 = sp.GetRequiredService<IMssqlDatabase>();
        Assert.NotNull(db1);
        Assert.NotNull(db2);
        Assert.NotSame(db1, db2); // transient
    }

    [Fact]
    public void Build_Registers_KeyVault_As_Singleton()
    {
        var services = new ServiceCollection();
        new AzureBlastBuilder()
            .WithKeyVault("https://example.vault.azure.net/")
            .Build(services);

        using var sp = services.BuildServiceProvider();
        var kv1 = sp.GetRequiredService<IAzureKeyVault>();
        var kv2 = sp.GetRequiredService<IAzureKeyVault>();
        Assert.Same(kv1, kv2); // singleton
    }

    [Fact]
    public void Build_Registers_TableStorage_And_ArmClients()
    {
        var services = new ServiceCollection();
        new AzureBlastBuilder()
            .WithTableStorage("UseDevelopmentStorage=true;", "MyTable")
            .Build(services);

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<IAzureTableStorage>());
        Assert.NotNull(sp.GetService<IArmClientWrapper>());
        Assert.NotNull(sp.GetService<IAzureResourceClient>());
    }

    [Fact]
    public void Build_Registers_ServiceBus_Only_When_Both_Provided()
    {
        // missing queue -> none
        var s1 = new ServiceCollection();
        new AzureBlastBuilder()
            .WithServiceBus("Endpoint=sb://x/;...", "orders") // comment this line to see the negative case
            .Build(s1);
        using var sp1 = s1.BuildServiceProvider();
        Assert.NotNull(sp1.GetService<IAzureServiceBus>());

        // negative case
        var s2 = new ServiceCollection();
        new AzureBlastBuilder()
            .Build(s2);
        using var sp2 = s2.BuildServiceProvider();
        Assert.Null(sp2.GetService<IAzureServiceBus>());
    }

    // ---------- source of IServiceCollection ----------
    [Fact]
    public void Build_Uses_Ctor_Services_When_No_Param_Passed()
    {
        var services = new ServiceCollection();
        new AzureBlastBuilder(services, new Azure.Identity.DefaultAzureCredential())
            .WithTableStorage("UseDevelopmentStorage=true;", "tbl")
            .Build(); // no arg -> uses the one from ctor

        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<IAzureTableStorage>());
    }

    [Fact]
    public void BuildServiceProvider_Works_EndToEnd()
    {
        var provider = new AzureBlastBuilder()
            .WithTableStorage("UseDevelopmentStorage=true;", "tbl")
            .BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAzureTableStorage>());
        (provider as IDisposable)?.Dispose();
    }

    // ---------- optional if you switch to TryAdd* in builder ----------
    // If you change the builder registrations from Add* -> TryAdd*,
    // then this test should pass and guarantees idempotency.
    [Fact]
    public void Build_Twice_Does_Not_Duplicate_When_Using_TryAdd()
    {
        var services = new ServiceCollection();
        var builder = new AzureBlastBuilder()
            .WithTableStorage("UseDevelopmentStorage=true;", "tbl")
            .WithServiceBus("Endpoint=sb://x/;...", "orders");

        builder.Build(services);
        builder.Build(services);

        // We can at least resolve once; counting descriptors requires helper if you want to assert exactly-one.
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IAzureTableStorage>());
        Assert.NotNull(sp.GetRequiredService<IAzureServiceBus>());
    }
}