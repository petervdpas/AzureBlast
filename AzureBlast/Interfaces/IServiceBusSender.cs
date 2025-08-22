using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace AzureBlast.Interfaces;

/// <summary>
/// Abstraction for <see cref="ServiceBusSender"/> to enable testing without
/// mocking sealed SDK types. Provides batch creation and send operations.
/// </summary>
public interface IServiceBusSender
{
    /// <summary>
    /// Creates a new message batch that can accept multiple messages according to size limits.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A testable <see cref="IMessageBatch"/> instance.</returns>
    Task<IMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a single message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a batch of messages previously filled in the provided <paramref name="batch"/>.
    /// </summary>
    /// <param name="batch">The message batch to send.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    Task SendMessagesAsync(IMessageBatch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a single message for future delivery.
    /// </summary>
    /// <param name="message">The message to schedule.</param>
    /// <param name="enqueueTime">The UTC time when the message should be enqueued.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset enqueueTime,
        CancellationToken cancellationToken = default);
}
