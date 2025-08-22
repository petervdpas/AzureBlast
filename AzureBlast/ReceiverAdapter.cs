using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Concrete adapter for <see cref="ServiceBusReceiver"/>.
/// </summary>
internal sealed class ReceiverAdapter : IReceiver
{
    private readonly ServiceBusReceiver _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceiverAdapter"/> class.
    /// </summary>
    /// <param name="inner">The underlying SDK receiver instance.</param>
    public ReceiverAdapter(ServiceBusReceiver inner) => _inner = inner;

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