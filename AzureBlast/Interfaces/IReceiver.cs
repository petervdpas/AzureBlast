using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace AzureBlast.Interfaces;

/// <summary>
/// Abstraction for receiving and completing messages (non‑session).
/// </summary>
public interface IReceiver
{
    /// <summary>
    /// Receives up to <paramref name="maxMessages"/> messages.
    /// </summary>
    /// <param name="maxMessages">Maximum number to receive.</param>
    /// <param name="maxWaitTime">Optional max wait time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of received messages or <c>null</c> if none.</returns>
    Task<IReadOnlyList<ServiceBusReceivedMessage>?> ReceiveMessagesAsync(
        int maxMessages,
        TimeSpan? maxWaitTime = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a message, marking it as processed.
    /// </summary>
    /// <param name="message">The message to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}