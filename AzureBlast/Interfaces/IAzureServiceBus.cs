using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace AzureBlast.Interfaces;

/// <summary>
///     Defines methods for interacting with Azure Service Bus, including sending, receiving, and managing messages.
/// </summary>
public interface IAzureServiceBus
{
    /// <summary>
    ///     Configures the Service Bus connection and sets the queue name for operations.
    /// </summary>
    /// <param name="connectionString">The connection string for the Service Bus namespace.</param>
    /// <param name="queueName">The name of the queue to interact with.</param>
    /// <param name="contentType">Optional. The content type of the messages, defaults to "application/json".</param>
    void Setup(string? connectionString, string? queueName, string contentType = "application/json");

    /// <summary>
    ///     Switches the active queue for sending and receiving messages.
    /// </summary>
    /// <param name="queueName">The new queue name to switch to.</param>
    void SwitchQueue(string? queueName);

    /// <summary>
    ///     Adds or updates a message property (header) that will be included in outgoing messages.
    /// </summary>
    /// <param name="key">The key of the property.</param>
    /// <param name="value">The value of the property.</param>
    void AddOrUpdateProperty(string key, object value);

    /// <summary>
    ///     Removes a message property (header) by its key.
    /// </summary>
    /// <param name="key">The key of the property to remove.</param>
    /// <returns>True if the property was removed successfully, otherwise false.</returns>
    bool RemoveProperty(string key);

    /// <summary>
    ///     Clears all message properties (headers).
    /// </summary>
    void ClearProperties();

    /// <summary>
    ///     Sends a single message to the specified Azure Service Bus queue.
    /// </summary>
    /// <param name="messageBody">The body of the message to send.</param>
    /// <param name="sessionId">Optional. The session ID for session-based messaging.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendMessageAsync(string messageBody, string? sessionId = null);

    /// <summary>
    ///     Sends a batch of messages to the specified Azure Service Bus queue.
    /// </summary>
    /// <param name="messages">The list of message bodies to send.</param>
    /// <param name="sessionId">Optional. The session ID for session-based messaging.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendBatchMessagesAsync(List<string> messages, string? sessionId = null);

    /// <summary>
    ///     Sends a message to the queue with scheduled delivery at a specified time.
    /// </summary>
    /// <param name="messageBody">The body of the message to send.</param>
    /// <param name="scheduleTimeUtc">The time at which the message should be delivered, in UTC.</param>
    /// <param name="sessionId">Optional. The session ID for session-based messaging.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendScheduledMessageAsync(string messageBody, DateTimeOffset scheduleTimeUtc, string? sessionId = null);

    /// <summary>
    ///     Receives messages from the specified Azure Service Bus queue.
    /// </summary>
    /// <param name="maxMessages">The maximum number of messages to receive in one call.</param>
    /// <param name="sessionId">Optional. The session ID for session-based messaging.</param>
    /// <returns>A list of messages received from the queue.</returns>
    Task<List<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages = 10, string? sessionId = null);

    /// <summary>
    ///     Marks a received message as complete, indicating that it has been processed successfully.
    /// </summary>
    /// <param name="message">The received message to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteMessageAsync(ServiceBusReceivedMessage message);
}
