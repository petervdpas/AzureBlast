using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AzureBlast.Extensions;
using AzureBlast.Tests.TestDoubles;

namespace AzureBlast.Tests;

public class BusSendExtensionsTests
{
    [Fact]
    public async Task SendJsonAsync_Single_AddsHeaders_CallsSend_RemovesHeaders()
    {
        var bus = new TestBus();
        var headers = new Dictionary<string, object> { ["h1"] = "v1", ["h2"] = 2 };

        await bus.SendJsonAsync(new { A = 1 }, headers, sessionId: "s-1");

        // call happened
        Assert.Single(bus.Singles);
        Assert.Equal("s-1", bus.Singles[0].SessionId);
        Assert.Contains("\"a\":1", bus.Singles[0].Body, StringComparison.OrdinalIgnoreCase);

        // headers removed
        Assert.Empty(bus.PropertyBag);
    }

    [Fact]
    public async Task SendJsonAsync_TupleOverload_Works_AndHeadersRemoved()
    {
        var bus = new TestBus();

        await bus.SendJsonAsync(new { X = 5 }, sessionId: null, json: null,
            ("tenant", "fontys"), ("region", "euw"));

        Assert.Single(bus.Singles);
        Assert.Contains("\"x\":5", bus.Singles[0].Body);
        Assert.Empty(bus.PropertyBag);
    }

    [Fact]
    public async Task SendJsonManyAsync_NoHeaders_UsesBatchOnce()
    {
        var bus = new TestBus();
        var payloads = Enumerable.Range(1, 3).Select(i => new { I = i });

        await bus.SendJsonManyAsync(payloads, headers: null, sessionId: "sess");

        Assert.Single(bus.Batches);
        Assert.Equal("sess", bus.Batches[0].SessionId);
        Assert.Equal(3, bus.Batches[0].Bodies.Count);
        Assert.All(new[] { "\"i\":1", "\"i\":2", "\"i\":3" }, s =>
            Assert.Contains(s, string.Join("|", bus.Batches[0].Bodies)));
        Assert.Empty(bus.PropertyBag);
    }

    [Fact]
    public async Task SendJsonManyAsync_WithBatchHeaders_AddsOnce_RemovesAfter()
    {
        var bus = new TestBus();
        var payloads = new[] { new { A = 1 }, new { A = 2 } };
        var headers = new Dictionary<string, object> { ["k"] = "v" };

        await bus.SendJsonManyAsync(payloads, headers, sessionId: "s");

        Assert.Single(bus.Batches);
        Assert.Equal(2, bus.Batches[0].Bodies.Count);
        // headers must be removed at end
        Assert.Empty(bus.PropertyBag);
    }

    [Fact]
    public async Task SendJsonManyAsync_HeaderFactory_SendsIndividually_PerPayloadHeaders()
    {
        var bus = new TestBus();
        var payloads = new object?[]
        {
            new { Kind = "A", N = 1 },
            new { Kind = "B", N = 2 }
        };

        await bus.SendJsonManyAsync(
            payloads,
            headerFactory: p =>
            {
                var kind = JsonDocument.Parse(JsonSerializer.Serialize(p))
                                       .RootElement.GetProperty("Kind").GetString();
                return new Dictionary<string, object> { ["kind"] = kind! };
            },
            sessionId: "S");

        // no batches, two singles
        Assert.Equal(2, bus.Singles.Count);

        // ensure headers were not left behind
        Assert.Empty(bus.PropertyBag);
    }

    [Fact]
    public async Task SendJsonAsync_CustomJsonOptions_WriteNullsTrue_PreservesNull()
    {
        var bus = new TestBus();
        var writeNulls = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        await bus.SendJsonWithHeadersAsync(new { A = (string?)null }, headers: null, sessionId: null, json: writeNulls);

        Assert.Single(bus.Singles);
        Assert.Contains("\"a\":null", bus.Singles[0].Body);
    }

    [Fact]
    public async Task SendJsonAsync_DefaultOptions_IgnoreNulls_DropsNull()
    {
        var bus = new TestBus();

        await bus.SendJsonAsync(new { A = (string?)null });

        Assert.Single(bus.Singles);
        Assert.DoesNotContain("\"a\":null", bus.Singles[0].Body);
        Assert.Equal("{}", bus.Singles[0].Body); // only property is null, so empty object
    }
}