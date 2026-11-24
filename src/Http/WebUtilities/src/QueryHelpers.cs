// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Provides methods for parsing and manipulating query strings.
/// </summary>
public static class QueryHelpers
{
    private const int StackallocCharThreshold = 256;

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

        var builder = new ValueStringBuilder(stackalloc char[StackallocCharThreshold]);
        char[]? rentedBuffer = null;
        try
        {
            Append(ref builder, ref rentedBuffer, uri.AsSpan(0, baseLength));
            Append(ref builder, ref rentedBuffer, separator);
            AppendUrlEncoded(ref builder, ref rentedBuffer, name);
            Append(ref builder, ref rentedBuffer, "=");
            AppendUrlEncoded(ref builder, ref rentedBuffer, value);
            Append(ref builder, ref rentedBuffer, uri.AsSpan(baseLength));
            return builder.AsSpan().ToString();
        }
        finally
        {
            ReturnRentedBuffer(builder.Length, rentedBuffer);
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

        var builder = new ValueStringBuilder(stackalloc char[StackallocCharThreshold]);
        char[]? rentedBuffer = null;
        try
        {
            Append(ref builder, ref rentedBuffer, uriToBeAppended);
            foreach (var parameter in queryString)
            {
                if (parameter.Value is null)
                {
                    continue;
                }

                Append(ref builder, ref rentedBuffer, hasQuery ? "&" : "?");
                AppendUrlEncoded(ref builder, ref rentedBuffer, parameter.Key);
                Append(ref builder, ref rentedBuffer, "=");
                AppendUrlEncoded(ref builder, ref rentedBuffer, parameter.Value);
                hasQuery = true;
            }

            Append(ref builder, ref rentedBuffer, anchorText);
            return builder.AsSpan().ToString();
        }
        finally
        {
            ReturnRentedBuffer(builder.Length, rentedBuffer);
        }
    }

    // ValueStringBuilder's own growth returns pooled arrays without clearing them, and query values may
    // contain sensitive data, so growth is done here and the builder only ever writes into a buffer it does not own.
    private static void Append(ref ValueStringBuilder builder, ref char[]? rentedBuffer, ReadOnlySpan<char> value)
    {
        if (value.Length > builder.Capacity - builder.Length)
        {
            Grow(ref builder, ref rentedBuffer, value.Length);
        }

        builder.Append(value);
    }

    private static void AppendUrlEncoded(ref ValueStringBuilder builder, ref char[]? rentedBuffer, string value)
    {
        var source = value.AsSpan();
        while (true)
        {
            var status = UrlEncoder.Default.Encode(source, builder.RawChars.Slice(builder.Length), out var charsConsumed, out var charsWritten, isFinalBlock: true);
            builder.Length += charsWritten;
            if (status != OperationStatus.DestinationTooSmall)
            {
                Debug.Assert(status == OperationStatus.Done);
                return;
            }

            source = source.Slice(charsConsumed);
            Grow(ref builder, ref rentedBuffer, source.Length);
        }
    }

    private static void Grow(ref ValueStringBuilder builder, ref char[]? rentedBuffer, int additionalCapacity)
    {
        var length = builder.Length;
        var newBuffer = ArrayPool<char>.Shared.Rent((int)Math.Max((uint)(length + additionalCapacity), (uint)builder.Capacity * 2));
        builder.AsSpan().CopyTo(newBuffer);
        ReturnRentedBuffer(length, rentedBuffer);
        rentedBuffer = newBuffer;
        builder = new ValueStringBuilder(newBuffer);
        builder.Length = length;
    }

    private static void ReturnRentedBuffer(int length, char[]? rentedBuffer)
    {
        if (rentedBuffer is not null)
        {
            rentedBuffer.AsSpan(0, length).Clear();
            ArrayPool<char>.Shared.Return(rentedBuffer);
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
