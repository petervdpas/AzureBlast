using System;
using System.Threading.Tasks;
using AzureBlast.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureBlast.Tests;

public class AzureBlastFactoryTests
{
    [Fact]
    public void CreateDatabase_Throws_On_Empty()
        => Assert.Throws<ArgumentException>(() => AzureBlastFactory.CreateDatabase(""));

    [Fact]
    public void CreateDatabase_Returns_Instance()
    {
        var db = AzureBlastFactory.CreateDatabase("Server=.;Database=foo;Trusted_Connection=True;");
        Assert.NotNull(db);
    }

    [Fact]
    public void CreateKeyVault_Throws_On_Empty_Url()
        => Assert.Throws<ArgumentException>(() => AzureBlastFactory.CreateKeyVault(""));

    [Fact]
    public void CreateKeyVault_Returns_Instance()
    {
        var kv = AzureBlastFactory.CreateKeyVault("https://example.vault.azure.net/");
        Assert.NotNull(kv);
    }

    [Fact]
    public async Task CreateKeyVaultAsync_Returns_Instance()
    {
        var kv = await AzureBlastFactory.CreateKeyVaultAsync("https://example.vault.azure.net/");
        Assert.NotNull(kv);
    }

    [Theory]
    [InlineData("", "q")]
    [InlineData("conn", "")]
    public void CreateServiceBus_Throws_On_Bad_Input(string conn, string queue)
        => Assert.Throws<ArgumentException>(() => AzureBlastFactory.CreateServiceBus(conn, queue));

    [Fact]
    public void CreateServiceBus_Returns_Instance()
    {
        var sb = AzureBlastFactory.CreateServiceBus("Endpoint=sb://x/;SharedAccessKeyName=Root;SharedAccessKey=K", "orders");
        Assert.NotNull(sb);
    }

    [Fact]
    public void CreateTableStorage_Throws_On_Empty_Conn()
        => Assert.Throws<ArgumentException>(() => AzureBlastFactory.CreateTableStorage("", "tbl"));

    [Fact]
    public void CreateTableStorage_Returns_Instance()
    {
        var ts = AzureBlastFactory.CreateTableStorage("UseDevelopmentStorage=true;", "MyTable");
        Assert.NotNull(ts);
    }

    [Fact]
    public void CreateArmClientWrapper_Returns_Instance()
    {
        var arm = AzureBlastFactory.CreateArmClientWrapper();
        Assert.NotNull(arm);
    }

    [Fact]
    public void CreateResourceClient_Returns_Instance()
    {
        var rc = AzureBlastFactory.CreateResourceClient();
        Assert.NotNull(rc);
    }

    [Fact]
    public void CreateServiceCollection_Registers_Services()
    {
        var sc = AzureBlastFactory.CreateServiceCollection(o =>
        {
            o.SqlConnectionString = "Server=.;Database=foo;Trusted_Connection=True;";
            o.KeyVaultUrl = "https://example.vault.azure.net/";
        });
        using var sp = sc.BuildServiceProvider();
        Assert.NotNull(sp.GetService<IMssqlDatabase>());
        Assert.NotNull(sp.GetService<IAzureKeyVault>());
    }

    [Fact]
    public void CreateServiceProvider_Builds_And_Resolves()
    {
        using var sp = AzureBlastFactory
            .CreateServiceCollection(o => o.TableStorageConnectionString = "UseDevelopmentStorage=true;")
            .BuildServiceProvider(); // returns ServiceProvider

        Assert.NotNull(sp.GetService<IAzureTableStorage>());
    }
}