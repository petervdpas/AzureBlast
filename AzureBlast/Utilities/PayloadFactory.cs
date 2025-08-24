using System;
using System.Collections.Generic;

namespace AzureBlast.Utilities;

/// <summary>
/// Factory helpers for creating common payload shapes.
/// All methods return plain objects that can be serialized to JSON.
/// </summary>
public static class PayloadFactory
{
    /// <summary>
    /// Creates a simple payload with a single property.
    /// </summary>
    /// <param name="key">Property name (must be non-empty).</param>
    /// <param name="value">Property value (maybe null).</param>
    /// <returns>An object suitable for JSON serialization.</returns>
    public static object Single(string key, object? value)
    {
        return string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Key must be non-empty", nameof(key))
            : new Dictionary<string, object?> { [key] = value };
    }

    /// <summary>
    /// Creates a payload with multiple key/value pairs.
    /// </summary>
    /// <param name="values">Pairs of keys and values. Null or empty keys are ignored.</param>
    /// <returns>An object suitable for JSON serialization.</returns>
    public static object Many(params (string Key, object? Value)[] values)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in values)
        {
            if (!string.IsNullOrWhiteSpace(k))
                dict[k] = v;
        }

        return dict;
    }

    /// <summary>
    /// Creates a standardized "event" payload with id, timestamp, and optional data.
    /// </summary>
    /// <param name="id">Event identifier.</param>
    /// <param name="timestampUtc">UTC timestamp for the event. Defaults to now.</param>
    /// <param name="data">Optional additional fields to include.</param>
    /// <returns>An object suitable for JSON serialization.</returns>
    public static object Event(string id, DateTimeOffset? timestampUtc = null,
        IDictionary<string, object?>? data = null)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = id,
            ["timestamp"] = (timestampUtc ?? DateTimeOffset.UtcNow).ToString("o")
        };

        if (data is null) return dict;
        
        foreach (var kv in data)
            dict[kv.Key] = kv.Value;

        return dict;
    }

    /// <summary>
    /// Creates a standardized "command" payload with type and optional data.
    /// </summary>
    /// <param name="commandType">Logical command type or name.</param>
    /// <param name="data">Optional fields to include.</param>
    /// <returns>An object suitable for JSON serialization.</returns>
    public static object Command(string commandType, IDictionary<string, object?>? data = null)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["commandType"] = commandType
        };

        if (data is null) return dict;
        
        foreach (var kv in data)
            dict[kv.Key] = kv.Value;

        return dict;
    }
}