// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Text.Encodings.Web;
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
        var separator = uri.AsSpan(0, baseLength).Contains('?') ? '&' : '?';
        var encodedName = UrlEncoder.Default.Encode(name);
        var encodedValue = UrlEncoder.Default.Encode(value);
        var length = uri.Length + encodedName.Length + encodedValue.Length + 2;

        return string.Create(length, (uri, baseLength, separator, encodedName, encodedValue), static (destination, state) =>
        {
            var (uri, baseLength, separator, encodedName, encodedValue) = state;
            uri.AsSpan(0, baseLength).CopyTo(destination);
            var position = baseLength;
            destination[position++] = separator;
            encodedName.CopyTo(destination.Slice(position));
            position += encodedName.Length;
            destination[position++] = '=';
            encodedValue.CopyTo(destination.Slice(position));
            position += encodedValue.Length;
            uri.AsSpan(baseLength).CopyTo(destination.Slice(position));
        });
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

        var builder = new PooledCharBuilder(uri.Length + 256);
        try
        {
            builder.Append(uriToBeAppended);
            foreach (var parameter in queryString)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                builder.Append(hasQuery ? '&' : '?');
                builder.Append(UrlEncoder.Default.Encode(parameter.Key));
                builder.Append('=');
                builder.Append(UrlEncoder.Default.Encode(parameter.Value));
                hasQuery = true;
            }

            builder.Append(anchorText);
            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }

    // Minimal ArrayPool-backed char builder; the rented buffer never escapes this type.
    private struct PooledCharBuilder
    {
        private char[] _buffer;
        private int _length;

        public PooledCharBuilder(int initialCapacity)
        {
            _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            _length = 0;
        }

        public void Append(char value)
        {
            if (_length == _buffer.Length)
            {
                Grow(1);
            }

            _buffer[_length++] = value;
        }

        public void Append(ReadOnlySpan<char> value)
        {
            if (value.Length > _buffer.Length - _length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_buffer.AsSpan(_length));
            _length += value.Length;
        }

        public override readonly string ToString() => new string(_buffer, 0, _length);

        public void Dispose()
        {
            var buffer = _buffer;
            var length = _length;
            _buffer = Array.Empty<char>();
            _length = 0;
            if (buffer.Length > 0)
            {
                // Query values may contain sensitive data.
                buffer.AsSpan(0, length).Clear();
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        private void Grow(int additional)
        {
            var newBuffer = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, _length + additional));
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            // Query values may contain sensitive data.
            _buffer.AsSpan(0, _length).Clear();
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = newBuffer;
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
            accumulator.Append(pair.DecodeName().ToString(), pair.DecodeValue().ToString());
        }

        if (!accumulator.HasValues)
        {
            return null;
        }

        return accumulator.GetResults();
    }
}
