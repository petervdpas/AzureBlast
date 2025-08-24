using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzureBlast.Interfaces;

namespace AzureBlast.Extensions;

/// <summary>
/// Provides convenience methods for sending JSON-serialized payloads via
/// an <see cref="IAzureServiceBus"/> instance.
/// <list type="bullet">
/// <item>Serializes objects to JSON (camelCase, ignore nulls by default).</item>
/// <item>Supports optional session IDs.</item>
/// <item>Allows attaching temporary application headers (added before send, removed afterward).</item>
/// <item>Supports batch sending when the same headers are used for all messages.</item>
/// <item>Supports dynamic per-payload headers via a factory delegate.</item>
/// </list>
/// </summary>
public static class BusSendExtensions
{
    private static readonly JsonSerializerOptions DefaultJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // ----- Single -----

    /// <summary>
    /// Serializes the specified <paramref name="payload"/> to JSON and sends it as a single message.
    /// Temporary headers are applied for sending only and removed afterward.
    /// </summary>
    /// <param name="bus">The <see cref="IAzureServiceBus"/> instance used to send the message.</param>
    /// <param name="payload">The object to serialize into the message body.</param>
    /// <param name="headers">
    /// Optional headers to apply to the message. Headers are added to the bus,
    /// used for sending, and then removed to avoid side effects.
    /// </param>
    /// <param name="sessionId">
    /// Optional session ID to assign to the message. If <c>null</c>, no session is applied.
    /// </param>
    /// <param name="json">
    /// Optional JSON serializer options. Defaults to camelCase and ignoring nulls.
    /// </param>
    /// <returns>Tasks representing the asynchronous send operation.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task SendJsonAsync(
        this IAzureServiceBus bus,
        object? payload,
        IDictionary<string, object>? headers = null,
        string? sessionId = null,
        JsonSerializerOptions? json = null)
    {
        var body = JsonSerializer.Serialize(payload, json ?? DefaultJson);
        Apply(bus, headers);
        try { await bus.SendMessageAsync(body, sessionId).ConfigureAwait(false); }
        finally { Remove(bus, headers); }
    }

    /// <summary>
    /// Convenience overload of <see cref="SendJsonAsync(IAzureServiceBus,object,System.Collections.Generic.IDictionary{string,object},string,System.Text.Json.JsonSerializerOptions)"/>
    /// that accepts headers as param tuples instead of a dictionary.
    /// </summary>
    /// <param name="bus">The <see cref="IAzureServiceBus"/> instance used to send the message.</param>
    /// <param name="payload">The object to serialize into the message body.</param>
    /// <param name="sessionId">Optional session ID to assign to the message.</param>
    /// <param name="json">Optional JSON serializer options.</param>
    /// <param name="headers">Headers to apply, specified as key-value tuples.</param>
    /// <returns>Tasks representing the asynchronous send operation.</returns>
    public static Task SendJsonAsync(
        this IAzureServiceBus bus,
        object? payload,
        string? sessionId = null,
        JsonSerializerOptions? json = null,
        params (string Key, object Value)[] headers)
        => SendJsonAsync(bus, payload, headers.ToDictionary(h => h.Key, h => h.Value), sessionId, json);

    // ----- Many -----

    /// <summary>
    /// Serializes and sends a collection of <paramref name="payloads"/> as a batch.
    /// If <paramref name="headers"/> are provided, they are applied to the entire batch
    /// (added before sending and removed afterward).
    /// </summary>
    /// <param name="bus">The <see cref="IAzureServiceBus"/> instance used to send the messages.</param>
    /// <param name="payloads">The collection of objects to serialize into message bodies.</param>
    /// <param name="headers">
    /// Optional headers to apply to all messages in the batch.
    /// Headers are added before sending and removed afterward.
    /// </param>
    /// <param name="sessionId">Optional session ID applied to the batch.</param>
    /// <param name="json">Optional JSON serializer options.</param>
    /// <returns>Tasks representing the asynchronous batch send operation.</returns>
    public static async Task SendJsonManyAsync(
        this IAzureServiceBus bus,
        IEnumerable<object?> payloads,
        IDictionary<string, object>? headers = null,
        string? sessionId = null,
        JsonSerializerOptions? json = null)
    {
        var bodies = payloads.Select(p => JsonSerializer.Serialize(p, json ?? DefaultJson)).ToList();
        if (bodies.Count == 0) return;

        if (headers is null || headers.Count == 0)
        {
            await bus.SendBatchMessagesAsync(bodies, sessionId).ConfigureAwait(false);
            return;
        }

        Apply(bus, headers);
        try { await bus.SendBatchMessagesAsync(bodies, sessionId).ConfigureAwait(false); }
        finally { Remove(bus, headers); }
    }

    /// <summary>
    /// Sends multiple <paramref name="payloads"/> with per-payload headers produced by a factory function.
    /// Each payload is serialized individually and sent with its own headers.
    /// </summary>
    /// <param name="bus">The <see cref="IAzureServiceBus"/> instance used to send the messages.</param>
    /// <param name="payloads">The collection of payloads to send.</param>
    /// <param name="headerFactory">
    /// Delegate that returns the headers to apply for a given payload.
    /// Returning <c>null</c> indicates no headers for that payload.
    /// </param>
    /// <param name="sessionId">Optional session ID applied to sending.</param>
    /// <param name="json">Optional JSON serializer options.</param>
    /// <returns>Tasks representing the asynchronous send operations.</returns>
    public static async Task SendJsonManyAsync(
        this IAzureServiceBus bus,
        IEnumerable<object?> payloads,
        Func<object?, IReadOnlyDictionary<string, object>?> headerFactory,
        string? sessionId = null,
        JsonSerializerOptions? json = null)
    {
        foreach (var p in payloads)
        {
            var headers = headerFactory?.Invoke(p);
            await bus.SendJsonAsync(p, headers is null ? null : new Dictionary<string, object>(headers),
                                    sessionId, json).ConfigureAwait(false);
        }
    }
    
    /// <summary>
    /// Convenience overload of <see cref="SendJsonAsync(IAzureServiceBus, object?, System.Collections.Generic.IDictionary{string, object}?, string?, System.Text.Json.JsonSerializerOptions?)"/>
    /// that accepts headers as param tuples with a distinct method name to avoid overload ambiguity.
    /// </summary>
    /// <param name="bus">The <see cref="IAzureServiceBus"/> instance used to send the message.</param>
    /// <param name="payload">The object to serialize into the message body.</param>
    /// <param name="sessionId">Optional session ID to assign to the message.</param>
    /// <param name="json">Optional JSON serializer options. Defaults to camelCase and ignoring nulls.</param>
    /// <param name="headers">Headers to apply, specified as key-value tuples.</param>
    /// <returns>Tasks representing the asynchronous send operation.</returns>
    public static Task SendJsonWithHeadersAsync(
        this IAzureServiceBus bus,
        object? payload,
        string? sessionId = null,
        JsonSerializerOptions? json = null,
        params (string Key, object Value)[]? headers)   // <-- nullable
    {
        var dict = (headers is null || headers.Length == 0)
            ? null
            : headers.ToDictionary(h => h.Key, h => h.Value);
        return SendJsonAsync(bus, payload, dict, sessionId, json);
    }

    // internals
    private static void Apply(IAzureServiceBus bus, IDictionary<string, object>? headers)
    {
        if (headers is null) return;
        foreach (var kv in headers) bus.AddOrUpdateProperty(kv.Key, kv.Value);
    }

    private static void Remove(IAzureServiceBus bus, IDictionary<string, object>? headers)
    {
        if (headers is null) return;
        foreach (var k in headers.Keys) bus.RemoveProperty(k);
    }
}
