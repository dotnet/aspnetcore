// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Provides methods for parsing and manipulating query strings.
/// </summary>
public static class QueryHelpers
{
    /// <summary>
    /// Append the given query key and value to the URI.
    /// </summary>
    /// <param name="uri">The base URI.</param>
    /// <param name="name">The name of the query key.</param>
    /// <param name="value">The query value.</param>
    /// <returns>The combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static string AddQueryString(string uri, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var anchorIndex = uri.IndexOf('#');
        var baseLength = anchorIndex < 0 ? uri.Length : anchorIndex;
        var separator = uri.AsSpan(0, baseLength).Contains('?') ? "&" : "?";

        Span<char> initialBuffer = stackalloc char[256];
        var writer = new RefPooledArrayBufferWriter<char>(initialBuffer);
        try
        {
            Append(ref writer, uri.AsSpan(0, baseLength));
            Append(ref writer, separator);
            AppendUrlEncoded(ref writer, name);
            Append(ref writer, "=");
            AppendUrlEncoded(ref writer, value);
            Append(ref writer, uri.AsSpan(baseLength));
            return writer.WrittenSpan.ToString();
        }
        finally
        {
            // Query values may contain sensitive data. Dispose clears any rented buffer, and only the start of the
            // stack buffer can have been written to.
            initialBuffer.Slice(0, Math.Min(writer.WrittenSpan.Length, initialBuffer.Length)).Clear();
            writer.Dispose();
        }
    }

    /// <summary>
    /// Append the given query keys and values to the URI.
    /// </summary>
    /// <param name="uri">The base URI.</param>
    /// <param name="queryString">A dictionary of query keys and values to append.</param>
    /// <returns>The combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="queryString"/> is <c>null</c>.</exception>
    public static string AddQueryString(string uri, IDictionary<string, string?> queryString)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(queryString);

        return AddQueryString(uri, (IEnumerable<KeyValuePair<string, string?>>)queryString);
    }

    /// <summary>
    /// Append the given query keys and values to the URI.
    /// </summary>
    /// <param name="uri">The base URI.</param>
    /// <param name="queryString">A collection of query names and values to append.</param>
    /// <returns>The combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="queryString"/> is <c>null</c>.</exception>
    public static string AddQueryString(string uri, IEnumerable<KeyValuePair<string, StringValues>> queryString)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(queryString);

        return AddQueryString(uri, queryString.SelectMany(kvp => kvp.Value, (kvp, v) => KeyValuePair.Create<string, string?>(kvp.Key, v)));
    }

    /// <summary>
    /// Append the given query keys and values to the URI.
    /// </summary>
    /// <param name="uri">The base URI.</param>
    /// <param name="queryString">A collection of name value query pairs to append.</param>
    /// <returns>The combined result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="queryString"/> is <c>null</c>.</exception>
    public static string AddQueryString(
        string uri,
        IEnumerable<KeyValuePair<string, string?>> queryString)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(queryString);

        var anchorIndex = uri.IndexOf('#');
        var uriToBeAppended = uri.AsSpan();
        var anchorText = ReadOnlySpan<char>.Empty;
        // If there is an anchor, then the query string must be inserted before its first occurrence.
        if (anchorIndex != -1)
        {
            anchorText = uriToBeAppended.Slice(anchorIndex);
            uriToBeAppended = uriToBeAppended.Slice(0, anchorIndex);
        }

        var queryIndex = uriToBeAppended.IndexOf('?');
        var hasQuery = queryIndex != -1;

        Span<char> initialBuffer = stackalloc char[256];
        var writer = new RefPooledArrayBufferWriter<char>(initialBuffer);
        try
        {
            Append(ref writer, uriToBeAppended);
            foreach (var parameter in queryString)
            {
                if (parameter.Value is null)
                {
                    continue;
                }

                Append(ref writer, hasQuery ? "&" : "?");
                AppendUrlEncoded(ref writer, parameter.Key);
                Append(ref writer, "=");
                AppendUrlEncoded(ref writer, parameter.Value);
                hasQuery = true;
            }

            Append(ref writer, anchorText);
            return writer.WrittenSpan.ToString();
        }
        finally
        {
            // Query values may contain sensitive data. Dispose clears any rented buffer, and only the start of the
            // stack buffer can have been written to.
            initialBuffer.Slice(0, Math.Min(writer.WrittenSpan.Length, initialBuffer.Length)).Clear();
            writer.Dispose();
        }
    }

    private static void Append(ref RefPooledArrayBufferWriter<char> writer, ReadOnlySpan<char> value)
    {
        // GetSpan(0) would request a default-sized buffer, so skip empty values.
        if (!value.IsEmpty)
        {
            value.CopyTo(writer.GetSpan(value.Length));
            writer.Advance(value.Length);
        }
    }

    private static void AppendUrlEncoded(ref RefPooledArrayBufferWriter<char> writer, string value)
    {
        var source = value.AsSpan();
        var sizeHint = source.Length;
        while (!source.IsEmpty)
        {
            var destination = writer.GetSpan(sizeHint);
            var status = UrlEncoder.Default.Encode(source, destination, out var charsConsumed, out var charsWritten, isFinalBlock: true);
            writer.Advance(charsWritten);
            if (status != OperationStatus.DestinationTooSmall)
            {
                Debug.Assert(status == OperationStatus.Done);
                return;
            }

            source = source.Slice(charsConsumed);
            // Request more than the space that was left so the writer grows.
            sizeHint = destination.Length - charsWritten + source.Length;
        }
    }

    /// <summary>
    /// Parse a query string into its component key and value parts.
    /// </summary>
    /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
    /// <returns>
    /// A collection of parsed keys and values. The returned <see cref="Dictionary{TKey, TValue}"/> always uses a
    /// case-insensitive (<see cref="StringComparer.OrdinalIgnoreCase"/>) key comparer so keys can be accessed
    /// in a case-insensitive manner regardless of whether the input contained any elements.
    /// </returns>
    public static Dictionary<string, StringValues> ParseQuery(string? queryString)
    {
        var result = ParseNullableQuery(queryString);

        if (result == null)
        {
            return new Dictionary<string, StringValues>(0, StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Parse a query string into its component key and value parts.
    /// </summary>
    /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
    /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
    public static Dictionary<string, StringValues>? ParseNullableQuery(string? queryString)
    {
        var accumulator = new KeyValueAccumulator();
        var enumerable = new QueryStringEnumerable(queryString);

        foreach (var pair in enumerable)
        {
            accumulator.Append(QueryStringDecoder.DecodeToString(pair.EncodedName), QueryStringDecoder.DecodeToString(pair.EncodedValue));
        }

        if (!accumulator.HasValues)
        {
            return null;
        }

        return accumulator.GetResults();
    }
}
