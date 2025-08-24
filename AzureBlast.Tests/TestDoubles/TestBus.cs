using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast.Tests.TestDoubles;

public sealed class TestBus : IAzureServiceBus
{
    // observable state
    public readonly Dictionary<string, object> PropertyBag = new(StringComparer.Ordinal);
    public readonly List<(string Body, string? SessionId)> Singles = new();
    public readonly List<(List<string> Bodies, string? SessionId)> Batches = new();

    // IAzureServiceBus members used by the extensions
    public Task SendMessageAsync(string messageBody, string? sessionId = null)
    {
        Singles.Add((messageBody, sessionId));
        return Task.CompletedTask;
    }

    public Task SendBatchMessagesAsync(List<string> messages, string? sessionId = null)
    {
        Batches.Add((messages, sessionId));
        return Task.CompletedTask;
    }

    public void AddOrUpdateProperty(string key, object value) => PropertyBag[key] = value;
    public bool RemoveProperty(string key) => PropertyBag.Remove(key);
    public void ClearProperties() => PropertyBag.Clear();

    // Unused in these tests — no-ops or throw if accidentally called
    public void Setup(string? connectionString, string? queueName, string contentType = "application/json") { }
    public void SwitchQueue(string? queueName) { }
    public Task SendScheduledMessageAsync(string messageBody, DateTimeOffset scheduleTimeUtc, string? sessionId = null) 
        => Task.CompletedTask;
    public Task<List<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages = 10, string? sessionId = null) 
        => Task.FromResult(new List<ServiceBusReceivedMessage>());
    public Task CompleteMessageAsync(ServiceBusReceivedMessage message) => Task.CompletedTask;
}