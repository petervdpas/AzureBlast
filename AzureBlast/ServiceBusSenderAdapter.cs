using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// Concrete adapter for <see cref="ServiceBusSender"/>.
/// </summary>
internal sealed class ServiceBusSenderAdapter : IServiceBusSender
{
    private readonly ServiceBusSender _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusSenderAdapter"/> class.
    /// </summary>
    /// <param name="inner">The underlying SDK sender instance.</param>
    public ServiceBusSenderAdapter(ServiceBusSender inner) => _inner = inner;

    /// <inheritdoc />
    public async Task<IMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default)
    {
        var batch = await _inner.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);
        return new MessageBatchAdapter(batch);
    }

    /// <inheritdoc />
    public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        => _inner.SendMessageAsync(message, cancellationToken);

    /// <inheritdoc />
    public Task SendMessagesAsync(IMessageBatch batch, CancellationToken cancellationToken = default)
    {
        var real = (batch as MessageBatchAdapter) ??
                   throw new InvalidOperationException("Unknown batch implementation.");
        return _inner.SendMessagesAsync(real.Inner, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset enqueueTime,
        CancellationToken cancellationToken = default)
        => _inner.ScheduleMessageAsync(message, enqueueTime.UtcDateTime, cancellationToken);
}
