using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Concrete adapter wrapping <see cref="ServiceBusMessageBatch"/>.
/// </summary>
internal sealed class MessageBatchAdapter : IMessageBatch
{
    private readonly ServiceBusMessageBatch _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBatchAdapter"/> class.
    /// </summary>
    /// <param name="inner">The underlying SDK batch instance.</param>
    public MessageBatchAdapter(ServiceBusMessageBatch inner) => _inner = inner;

    /// <inheritdoc />
    public int Count => _inner.Count;

    /// <inheritdoc />
    public bool TryAddMessage(ServiceBusMessage message) => _inner.TryAddMessage(message);

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();

    /// <summary>
    /// Gets the underlying SDK batch. Used internally by the sender adapter.
    /// </summary>
    internal ServiceBusMessageBatch Inner => _inner;
}