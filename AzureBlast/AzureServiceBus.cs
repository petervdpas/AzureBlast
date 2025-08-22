using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using AzureBlast.Interfaces;

namespace AzureBlast;

/// <summary>
/// A helper class to interact with Azure Service Bus. Provides methods for sending and receiving the messages,
/// managing message properties (headers), and scheduling messages. Implements <see cref="IAzureServiceBus"/>.
/// </summary>
public class AzureServiceBus : IAzureServiceBus
{
    private readonly ServiceBusClient? _client;
    private readonly Dictionary<string, object> _properties = new();

    private readonly Func<ServiceBusClient, string, IServiceBusSender> _senderFactory;
    private readonly Func<ServiceBusClient, string, IReceiver> _receiverFactory;
    private readonly Func<ServiceBusClient, string, string, ISessionReceiver> _sessionReceiverFactory;

    private string? _connectionString;
    private string? _queueName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBus"/> class with default settings.
    /// Uses internally created <see cref="ServiceBusClient"/> when needed.
    /// </summary>
    public AzureServiceBus()
        : this(client: null, senderFactory: null, receiverFactory: null, sessionReceiverFactory: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBus"/> class with an optional injected client and factories.
    /// This is ideal for dependency injection and testing, allowing custom or mocked adapter factories to be provided.
    /// </summary>
    /// <param name="client">Optional <see cref="ServiceBusClient"/> to use for operations.</param>
    /// <param name="senderFactory">Factory that creates <see cref="IServiceBusSender"/> form a client and queue name.</param>
    /// <param name="receiverFactory">Factory that creates <see cref="IReceiver"/> from a client and queue name.</param>
    /// <param name="sessionReceiverFactory">Factory that creates <see cref="ISessionReceiver"/> from a client, queue, and session id.</param>
    public AzureServiceBus(
        ServiceBusClient? client,
        Func<ServiceBusClient, string, IServiceBusSender>? senderFactory = null,
        Func<ServiceBusClient, string, IReceiver>? receiverFactory = null,
        Func<ServiceBusClient, string, string, ISessionReceiver>? sessionReceiverFactory = null)
    {
        _client = client;
        _senderFactory = senderFactory ?? ((c, q) => new ServiceBusSenderAdapter(c.CreateSender(q)));
        _receiverFactory = receiverFactory ?? ((c, q) => new ReceiverAdapter(c.CreateReceiver(q)));
        _sessionReceiverFactory = sessionReceiverFactory ??
                                  ((c, q, s) =>
                                      new SessionReceiverAdapter(c.AcceptSessionAsync(q, s).GetAwaiter().GetResult()));
    }

    /// <summary>
    /// Gets or sets the default content type applied to outgoing messages (defaults to <c>application/json</c>).
    /// </summary>
    private string ContentType { get; set; } = "application/json";

    /// <inheritdoc />
    public void Setup(string? connectionString, string? queueName, string contentType = "application/json")
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        ContentType = string.IsNullOrEmpty(contentType) ? "application/json" : contentType;
    }

    /// <inheritdoc />
    public void SwitchQueue(string? queueName)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string must be set via Setup() before switching queues.");

        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentNullException(nameof(queueName), "Queue name cannot be null or empty.");

        _queueName = queueName;
    }

    /// <inheritdoc />
    public void AddOrUpdateProperty(string key, object value) => _properties[key] = value;

    /// <inheritdoc />
    public bool RemoveProperty(string key) => _properties.Remove(key);

    /// <inheritdoc />
    public void ClearProperties() => _properties.Clear();

    /// <inheritdoc />
    public async Task SendMessageAsync(string messageBody, string? sessionId = null)
    {
        EnsureConfigured();

        var client = CreateServiceBusClient();
        var sender = _senderFactory(client, _queueName!);

        var message = new ServiceBusMessage(messageBody)
        {
            ContentType = ContentType
        };

        if (!string.IsNullOrEmpty(sessionId))
            message.SessionId = sessionId;

        foreach (var property in _properties)
            message.ApplicationProperties[property.Key] = property.Value;

        await sender.SendMessageAsync(message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendBatchMessagesAsync(List<string> messages, string? sessionId = null)
    {
        EnsureConfigured();
        var client = CreateServiceBusClient();
        var sender = _senderFactory(client, _queueName!);

        int index = 0;

        while (index < messages.Count)
        {
            // NOTE: IMessageBatch is IDisposable, not IAsyncDisposable → use 'using', not 'await using'
            using var batch = await sender.CreateMessageBatchAsync().ConfigureAwait(false);

            for (; index < messages.Count; index++)
            {
                var body = messages[index];
                var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(body))
                {
                    ContentType = ContentType
                };

                if (!string.IsNullOrEmpty(sessionId))
                    msg.SessionId = sessionId;

                foreach (var property in _properties)
                    msg.ApplicationProperties[property.Key] = property.Value;

                if (!batch.TryAddMessage(msg))
                    break; // batch full
            }

            await sender.SendMessagesAsync(batch).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SendScheduledMessageAsync(string messageBody, DateTimeOffset scheduleTimeUtc,
        string? sessionId = null)
    {
        EnsureConfigured();

        var client = CreateServiceBusClient();
        var sender = _senderFactory(client, _queueName!);

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
        {
            ContentType = ContentType,
            ScheduledEnqueueTime = scheduleTimeUtc.UtcDateTime
        };

        if (!string.IsNullOrEmpty(sessionId))
            message.SessionId = sessionId;

        foreach (var property in _properties)
            message.ApplicationProperties[property.Key] = property.Value;

        _ = await sender.ScheduleMessageAsync(message, scheduleTimeUtc).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages = 10,
        string? sessionId = null)
    {
        EnsureConfigured();

        var client = CreateServiceBusClient();

        IReceiver receiver = sessionId is null
            ? _receiverFactory(client, _queueName!)
            : _sessionReceiverFactory(client, _queueName!, sessionId);

        var messages = await receiver.ReceiveMessagesAsync(maxMessages).ConfigureAwait(false);
        return messages?.ToList() ?? new List<ServiceBusReceivedMessage>();
    }

    /// <inheritdoc />
    public async Task CompleteMessageAsync(ServiceBusReceivedMessage message)
    {
        EnsureConfigured();

        var client = CreateServiceBusClient();
        var receiver = _receiverFactory(client, _queueName!);
        await receiver.CompleteMessageAsync(message).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures that the Azure Service Bus is properly configured before any operations are performed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the connection string or queue name has not been set by <see cref="Setup(string?, string?, string)"/>.
    /// </exception>
    private void EnsureConfigured()
    {
        if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_queueName))
            throw new InvalidOperationException(
                "AzureServiceBus is not configured. Call Setup() before performing any operations.");
    }

    /// <summary>
    /// Creates and returns the <see cref="ServiceBusClient"/> to use for operations.
    /// Returns the injected client if one was provided, otherwise creates a new instance.
    /// </summary>
    private ServiceBusClient CreateServiceBusClient()
    {
        if (_client != null)
            return _client; // Use the injected client for testing

        var options = new ServiceBusClientOptions
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };

        return new ServiceBusClient(_connectionString, options);
    }
}