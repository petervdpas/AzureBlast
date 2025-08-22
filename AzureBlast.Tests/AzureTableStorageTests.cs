using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Moq;

namespace AzureBlast.Tests;

public class AzureTableStorageTests
{
    private static AzureTableStorage CreateSut(
        out Mock<TableServiceClient> serviceMock,
        out Mock<TableClient> tableMock)
    {
        var s = new Mock<TableServiceClient>(MockBehavior.Strict);
        var t = new Mock<TableClient>(MockBehavior.Strict);

        TableServiceClient ServiceFactory(string _) => s.Object;
        TableClient TableFactory(string s1, string s2) => t.Object;

        serviceMock = s;
        tableMock = t;

        return new AzureTableStorage(ServiceFactory, TableFactory);
    }

    // Helper AsyncPageable to yield items
    private sealed class TestAsyncPageable<T>(IEnumerable<T> items) : AsyncPageable<T>
        where T : notnull
    {
        private readonly IReadOnlyList<T> _items = items.ToList();

        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null,
            int? pageSizeHint = null)
        {
            yield return Page<T>.FromValues(_items, null, Mock.Of<Response>());
            await Task.CompletedTask;
        }
    }

    [Fact]
    public void Initialize_Sets_Service_And_Optional_Table()
    {
        var sut = CreateSut(out _, out _);

        // When tableName provided, SetTable should be called → we expect a TableClient to be created via factory
        sut.Initialize("UseDevelopmentStorage=true;", "MyTable");

        // nothing to assert directly; follow-up operations will reveal misconfigured
    }

    [Fact]
    public async Task ListTablesAsync_Returns_Names()
    {
        var sut = CreateSut(out var service, out _);
        sut.Initialize("conn", null);

        var items = new[]
        {
            TableModelFactory.TableItem("t1"),
            TableModelFactory.TableItem("t2")
        };

        service
            .Setup(s => s.QueryAsync(
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncPageable<TableItem>(items));

        var result = await sut.ListTablesAsync();

        Assert.Equal(new[] { "t1", "t2" }, result);
    }

    [Fact]
    public void SetTable_After_Initialize_Configures_TableClient()
    {
        var sut = CreateSut(out _, out _);
        sut.Initialize("conn", null);

        // Should not throw
        sut.SetTable("AnotherTable");
    }

    private class DemoEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    [Fact]
    public async Task UpsertEntityAsync_Calls_TableClient()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        var entity = new DemoEntity { PartitionKey = "p", RowKey = "r" };

        table
            .Setup(t => t.UpsertEntityAsync(entity, TableUpdateMode.Merge, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue((Response?)null, Mock.Of<Response>())); // overload returns Response

        await sut.UpsertEntityAsync(entity);

        table.VerifyAll();
    }

    [Fact]
    public async Task DeleteEntityAsync_Calls_TableClient()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        table
            .Setup(t => t.DeleteEntityAsync("p", "r", It.IsAny<ETag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue((Response?)null, Mock.Of<Response>()));

        await sut.DeleteEntityAsync("p", "r");

        table.VerifyAll();
    }

    [Fact]
    public async Task GetEntityAsync_Returns_Entity_When_Found()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        var entity = new DemoEntity { PartitionKey = "p", RowKey = "r" };

        table
            .Setup(t => t.GetEntityAsync<DemoEntity>(
                "p", "r",
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(entity, Mock.Of<Response>()));

        var result = await sut.GetEntityAsync<DemoEntity>("p", "r");

        Assert.NotNull(result);
        Assert.Equal("p", result.PartitionKey);
        Assert.Equal("r", result.RowKey);
    }

    [Fact]
    public async Task GetEntityAsync_Returns_Null_On_404()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        table
            .Setup(t => t.GetEntityAsync<DemoEntity>(
                "p", "missing",
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not found"));

        var result = await sut.GetEntityAsync<DemoEntity>("p", "missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task QueryEntitiesAsync_Returns_Results()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        var e1 = new DemoEntity { PartitionKey = "p", RowKey = "r1" };
        var e2 = new DemoEntity { PartitionKey = "p", RowKey = "r2" };

        table
            .Setup(t => t.QueryAsync<DemoEntity>(
                "PartitionKey eq 'p'",
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncPageable<DemoEntity>([e1, e2]));

        var results = await sut.QueryEntitiesAsync<DemoEntity>("PartitionKey eq 'p'");
        Assert.Equal(2, results.Count);
        Assert.Contains(results, e => e.RowKey == "r1");
        Assert.Contains(results, e => e.RowKey == "r2");
    }

    [Fact]
    public async Task CheckEntitiesExistAsync_Returns_Only_Found_RowKeys()
    {
        var sut = CreateSut(out _, out var table);
        sut.Initialize("conn", "T");

        // For rowKey 'hit', return one entity; for 'miss', return none
        table
            .Setup(t => t.QueryAsync<DemoEntity>(
                "RowKey eq 'hit'",
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncPageable<DemoEntity>([
                new DemoEntity { PartitionKey = "p", RowKey = "hit" }
            ]));

        table
            .Setup(t => t.QueryAsync<DemoEntity>(
                "RowKey eq 'miss'",
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncPageable<DemoEntity>([]));

        var found = await sut.CheckEntitiesExistAsync<DemoEntity>(["hit", "miss"]);

        Assert.Single(found);
        Assert.Equal("hit", found[0]);
    }

    [Fact]
    public async Task Methods_Throw_If_Not_Initialized()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ListTablesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpsertEntityAsync(new DemoEntity()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteEntityAsync("p", "r"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetEntityAsync<DemoEntity>("p", "r"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.QueryEntitiesAsync<DemoEntity>("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CheckEntitiesExistAsync<DemoEntity>(new()));
    }
}