using System;
using Azure.Messaging.ServiceBus;

namespace AzureBlast.Interfaces;

/// <summary>
/// Abstraction for a message batch, used to decouple tests from sealed SDK types.
/// </summary>
public interface IMessageBatch : IDisposable
{
    /// <summary>
    /// Gets the count of messages currently in the batch.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Attempts to add a message to the batch, respecting size constraints.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <returns><c>true</c> if the message was added; otherwise <c>false</c>.</returns>
    bool TryAddMessage(ServiceBusMessage message);
}