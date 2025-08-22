using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Concrete adapter for <see cref="ServiceBusSessionReceiver"/>.
/// </summary>
internal sealed class SessionReceiverAdapter : ISessionReceiver
{
    private readonly ServiceBusSessionReceiver _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionReceiverAdapter"/> class.
    /// </summary>
    /// <param name="inner">The underlying SDK session receiver instance.</param>
    public SessionReceiverAdapter(ServiceBusSessionReceiver inner) => _inner = inner;

    /// <inheritdoc />
    public Task<IReadOnlyList<ServiceBusReceivedMessage>?> ReceiveMessagesAsync(
        int maxMessages,
        TimeSpan? maxWaitTime = default,
        CancellationToken cancellationToken = default)
        => _inner.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken);

    /// <inheritdoc />
    public Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        => _inner.CompleteMessageAsync(message, cancellationToken);
}