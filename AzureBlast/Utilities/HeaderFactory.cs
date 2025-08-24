using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureBlast.Utilities;

/// <summary>
/// Factory helpers for creating and merging header dictionaries.
/// <para>
/// All methods are transport/framework-agnostic. They return fresh
/// read-only snapshots (<see cref="IReadOnlyDictionary{TKey,TValue}"/>),
/// ensuring immutability for the caller.
/// </para>
/// </summary>
public static class HeaderFactory
{
    /// <summary>
    /// Returns an empty, read-only header dictionary.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IReadOnlyDictionary<string, object> Empty()
        => new Dictionary<string, object>(0, StringComparer.Ordinal);

    /// <summary>
    /// Creates a header dictionary from key/value tuples.
    /// <list type="bullet">
    /// <item>Ignores null or whitespace keys.</item>
    /// <item>If a key appears multiple times, the last value wins.</item>
    /// </list>
    /// </summary>
    /// <param name="headers">Key/value tuples to include as headers.</param>
    /// <returns>A read-only header dictionary.</returns>
    public static IReadOnlyDictionary<string, object> Create(params (string Key, object Value)[]? headers)
        => headers is null
            ? Empty()
            : new Dictionary<string, object>(
                headers.Where(h => !string.IsNullOrWhiteSpace(h.Key))
                       .GroupBy(h => h.Key, StringComparer.Ordinal)
                       .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.Ordinal),
                StringComparer.Ordinal);

    /// <summary>
    /// Creates a read-only copy of the provided <paramref name="source"/> dictionary.
    /// </summary>
    /// <param name="source">The source dictionary, or <c>null</c> for empty.</param>
    /// <returns>A read-only copy of the source dictionary.</returns>
    public static IReadOnlyDictionary<string, object> From(IDictionary<string, object>? source)
        => source is null
            ? Empty()
            : new Dictionary<string, object>(source, StringComparer.Ordinal);

    /// <summary>
    /// Returns a new dictionary with <paramref name="overrides"/> applied
    /// on top of <paramref name="baseHeaders"/>.
    /// <list type="bullet">
    /// <item>If both are null/empty, an empty dictionary is returned.</item>
    /// <item>If keys overlap, the <paramref name="overrides"/> values take precedence.</item>
    /// </list>
    /// </summary>
    /// <param name="baseHeaders">The base set of headers (maybe null).</param>
    /// <param name="overrides">Headers to apply on top of the base set (maybe null).</param>
    /// <returns>A merged read-only header dictionary.</returns>
    public static IReadOnlyDictionary<string, object> Merge(
        IReadOnlyDictionary<string, object>? baseHeaders,
        IReadOnlyDictionary<string, object>? overrides)
    {
        if ((baseHeaders is null || baseHeaders.Count == 0) &&
            (overrides is null || overrides.Count == 0))
            return Empty();

        var result = baseHeaders is null
            ? new Dictionary<string, object>(StringComparer.Ordinal)
            : new Dictionary<string, object>(baseHeaders, StringComparer.Ordinal);

        if (overrides is null) return result;
        
        foreach (var kv in overrides) result[kv.Key] = kv.Value;

        return result;
    }
}
