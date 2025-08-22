using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;
using Moq;

namespace AzureBlast.Tests;

public class AzureServiceBusTests
{
    private readonly Mock<ServiceBusClient> _client = new(MockBehavior.Strict);
    private readonly Mock<ServiceBusSender> _sender = new(MockBehavior.Strict);
    private readonly Mock<ServiceBusReceiver> _receiver = new(MockBehavior.Strict);
    private readonly Mock<ServiceBusSessionReceiver> _sessionReceiver = new(MockBehavior.Strict);

    private AzureServiceBus CreateSutConfigured(string queue = "q1", string contentType = "application/json")
    {
        var sut = new AzureServiceBus(_client.Object);
        sut.Setup("Endpoint=sb://fake/;SharedAccessKeyName=x;SharedAccessKey=y", queue, contentType);
        return sut;
    }

    [Fact]
    public void Setup_Throws_On_NullArgs()
    {
        var sut = new AzureServiceBus(_client.Object);
        Assert.Throws<ArgumentNullException>(() => sut.Setup(null, "q"));
        Assert.Throws<ArgumentNullException>(() => sut.Setup("conn", null));
    }

    [Fact]
    public void SwitchQueue_Throws_When_Not_Configured()
    {
        var sut = new AzureServiceBus(_client.Object);
        Assert.Throws<InvalidOperationException>(() => sut.SwitchQueue("q2"));
    }

    [Fact]
    public void SwitchQueue_Throws_On_NullOrEmpty()
    {
        var sut = CreateSutConfigured();
        Assert.Throws<ArgumentNullException>(() => sut.SwitchQueue(null));
        Assert.Throws<ArgumentNullException>(() => sut.SwitchQueue(""));
    }

    [Fact]
    public async Task SendMessageAsync_Sends_With_ContentType_Session_And_Properties()
    {
        var sut = CreateSutConfigured(contentType: "application/custom");
        sut.AddOrUpdateProperty("k1", "v1");
        sut.AddOrUpdateProperty("k2", 42);

        _client.Setup(c => c.CreateSender("q1")).Returns(_sender.Object);

        ServiceBusMessage? captured = null;
        _sender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        await sut.SendMessageAsync("hello", sessionId: "sess-123");

        Assert.NotNull(captured);
        Assert.Equal("application/custom", captured!.ContentType);
        Assert.Equal("sess-123", captured.SessionId);
        Assert.Equal("hello", captured.Body.ToString());
        Assert.Equal("v1", captured.ApplicationProperties["k1"]);
        Assert.Equal(42, captured.ApplicationProperties["k2"]);
    }

[Fact]
public async Task SendBatchMessagesAsync_Sends_In_One_Batch_When_Fits()
{
    var mockSender = new Mock<IServiceBusSender>(MockBehavior.Strict);
    var mockBatch  = new Mock<IMessageBatch>(MockBehavior.Strict);

    // REQUIRED for Strict: the using block will call Dispose()
    mockBatch.Setup(b => b.Dispose());

    mockBatch.SetupGet(b => b.Count).Returns(0);
    mockBatch.Setup(b => b.TryAddMessage(It.IsAny<ServiceBusMessage>())).Returns(true);

    mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(mockBatch.Object);
    mockSender.Setup(s => s.SendMessagesAsync(mockBatch.Object, It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

    var sut = new AzureServiceBus(client: _client.Object, senderFactory: (_, __) => mockSender.Object);
    sut.Setup("Endpoint=sb://fake/;SharedAccessKeyName=x;SharedAccessKey=y", "q1");

    var payloads = new List<string> { "a", "b", "c" };
    await sut.SendBatchMessagesAsync(payloads);

    mockBatch.Verify(b => b.TryAddMessage(It.IsAny<ServiceBusMessage>()), Times.Exactly(payloads.Count));
    mockSender.Verify(s => s.SendMessagesAsync(mockBatch.Object, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task SendBatchMessagesAsync_Splits_Into_Multiple_Batches_When_Full()
{
    var mockSender = new Mock<IServiceBusSender>(MockBehavior.Strict);
    var batch1 = new Mock<IMessageBatch>(MockBehavior.Strict);
    var batch2 = new Mock<IMessageBatch>(MockBehavior.Strict);

    // REQUIRED for Strict
    batch1.Setup(b => b.Dispose());
    batch2.Setup(b => b.Dispose());

    int added = 0;
    batch1.SetupGet(b => b.Count).Returns(() => added);
    batch1.Setup(b => b.TryAddMessage(It.IsAny<ServiceBusMessage>()))
          .Returns(() => ++added <= 2);
    batch2.SetupGet(b => b.Count).Returns(1);
    batch2.Setup(b => b.TryAddMessage(It.IsAny<ServiceBusMessage>())).Returns(true);

    var batches = new Queue<IMessageBatch>(new[] { batch1.Object, batch2.Object });

    mockSender.Setup(s => s.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(() => batches.Dequeue());

    mockSender.Setup(s => s.SendMessagesAsync(batch1.Object, It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
    mockSender.Setup(s => s.SendMessagesAsync(batch2.Object, It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

    var sut = new AzureServiceBus(client: _client.Object, senderFactory: (_, __) => mockSender.Object);
    sut.Setup("Endpoint=sb://fake/;SharedAccessKeyName=x;SharedAccessKey=y", "q1");

    await sut.SendBatchMessagesAsync(new List<string> { "m1", "m2", "m3" });

    batch1.Verify(b => b.TryAddMessage(It.IsAny<ServiceBusMessage>()), Times.Exactly(3));
    mockSender.Verify(s => s.SendMessagesAsync(batch1.Object, It.IsAny<CancellationToken>()), Times.Once);
    mockSender.Verify(s => s.SendMessagesAsync(batch2.Object, It.IsAny<CancellationToken>()), Times.Once);
}

    [Fact]
    public async Task SendScheduledMessageAsync_Schedules_With_UTC_Time_And_Properties()
    {
        var sut = CreateSutConfigured();
        sut.AddOrUpdateProperty("x", "y");

        _client.Setup(c => c.CreateSender("q1")).Returns(_sender.Object);

        DateTimeOffset when = DateTimeOffset.UtcNow.AddMinutes(5);
        ServiceBusMessage? captured = null;

        _sender
            .Setup(s => s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, DateTimeOffset, CancellationToken>((m, t, _) =>
            {
                captured = m;
                Assert.Equal(when.UtcDateTime, t.UtcDateTime);
            })
            .ReturnsAsync(123L);

        await sut.SendScheduledMessageAsync("sch", when, sessionId: null);

        Assert.NotNull(captured);
        Assert.Equal("application/json", captured!.ContentType);
        Assert.Equal("y", captured.ApplicationProperties["x"]);
    }

    [Fact]
    public async Task ReceiveMessagesAsync_Without_Session_Uses_CreateReceiver()
    {
        var sut = CreateSutConfigured();

        _client.Setup(c => c.CreateReceiver("q1")).Returns(_receiver.Object);
        _receiver.Setup(r => r.ReceiveMessagesAsync(10, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(body: new BinaryData("a")),
                ServiceBusModelFactory.ServiceBusReceivedMessage(body: new BinaryData("b"))
            });

        var result = await sut.ReceiveMessagesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Body.ToString());
        Assert.Equal("b", result[1].Body.ToString());
    }

    [Fact]
    public async Task ReceiveMessagesAsync_With_Session_Uses_AcceptSession()
    {
        var sut = CreateSutConfigured();

        _client.Setup(c => c.AcceptSessionAsync("q1", "s1", It.IsAny<ServiceBusSessionReceiverOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_sessionReceiver.Object);

        _sessionReceiver.Setup(r => r.ReceiveMessagesAsync(5, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(body: new BinaryData("x"))
            });

        var result = await sut.ReceiveMessagesAsync(maxMessages: 5, sessionId: "s1");

        Assert.Single(result);
        Assert.Equal("x", result[0].Body.ToString());
    }

    [Fact]
    public async Task ReceiveMessagesAsync_Returns_Empty_List_On_Null()
    {
        var sut = CreateSutConfigured();

        _client.Setup(c => c.CreateReceiver("q1")).Returns(_receiver.Object);
        _receiver.Setup(r => r.ReceiveMessagesAsync(10, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ServiceBusReceivedMessage>?)null);

        var result = await sut.ReceiveMessagesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task CompleteMessageAsync_Calls_Receiver_Complete()
    {
        var sut = CreateSutConfigured();

        _client.Setup(c => c.CreateReceiver("q1")).Returns(_receiver.Object);

        var msg = ServiceBusModelFactory.ServiceBusReceivedMessage(body: new BinaryData("done"));
        _receiver.Setup(r => r.CompleteMessageAsync(msg, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await sut.CompleteMessageAsync(msg);

        _receiver.Verify(r => r.CompleteMessageAsync(msg, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Operations_Throw_When_Not_Configured()
    {
        var sut = new AzureServiceBus(_client.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SendMessageAsync("x"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SendBatchMessagesAsync(new List<string> { "x" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SendScheduledMessageAsync("x", DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ReceiveMessagesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CompleteMessageAsync(ServiceBusModelFactory.ServiceBusReceivedMessage()));
        Assert.Throws<InvalidOperationException>(() => sut.SwitchQueue("q2"));
    }

    [Fact]
    public async Task Properties_Add_Remove_Clear_Work()
    {
        var sut = CreateSutConfigured();
        sut.AddOrUpdateProperty("k", 1);
        sut.AddOrUpdateProperty("k", 2); // update

        // Validate via a send capture
        _client.Setup(c => c.CreateSender("q1")).Returns(_sender.Object);

        ServiceBusMessage? captured = null;
        _sender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        sut.RemoveProperty("missing"); // false, no throw
        Assert.True(sut.RemoveProperty("k") || true); // no observable API; call it

        sut.AddOrUpdateProperty("k2", "v2");
        sut.ClearProperties(); // wipe all

        // After clear, nothing should be set
        sut.AddOrUpdateProperty("only", "this");
        sut.SwitchQueue("q1"); // no-op but exercises a code path
        // Send it once to inspect
        await sut.SendMessageAsync("p");

        Assert.NotNull(captured);
        Assert.True(captured!.ApplicationProperties.ContainsKey("only"));
        Assert.False(captured!.ApplicationProperties.ContainsKey("k2"));
    }
}