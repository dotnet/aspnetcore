// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides correct handling for QueryString value when needed to reconstruct a request or redirect URI string
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct QueryString : IEquatable<QueryString>
{
    /// <summary>
    /// Represents the empty query string. This field is read-only.
    /// </summary>
    public static readonly QueryString Empty = new QueryString(string.Empty);

    /// <summary>
    /// Initialize the query string with a given value. This value must be in escaped and delimited format with
    /// a leading '?' character.
    /// </summary>
    /// <param name="value">The query string to be assigned to the Value property.</param>
    public QueryString(string? value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '?')
        {
            throw new ArgumentException("The leading '?' must be included for a non-empty query.", nameof(value));
        }
        Value = value;
    }

    /// <summary>
    /// The escaped query string with the leading '?' character
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// True if the query string is not empty
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Provides the query string escaped in a way which is correct for combining into the URI representation.
    /// A leading '?' character will be included unless the Value is null or empty. Characters which are potentially
    /// dangerous are escaped.
    /// </summary>
    /// <returns>The query string value</returns>
    public override string ToString()
    {
        return ToUriComponent();
    }

    /// <summary>
    /// Provides the query string escaped in a way which is correct for combining into the URI representation.
    /// A leading '?' character will be included unless the Value is null or empty. Characters which are potentially
    /// dangerous are escaped.
    /// </summary>
    /// <returns>The query string value</returns>
    public string ToUriComponent()
    {
        // Escape things properly so System.Uri doesn't mis-interpret the data.
        return HasValue ? Value.Replace("#", "%23") : string.Empty;
    }

    /// <summary>
    /// Returns an QueryString given the query as it is escaped in the URI format. The string MUST NOT contain any
    /// value that is not a query.
    /// </summary>
    /// <param name="uriComponent">The escaped query as it appears in the URI format.</param>
    /// <returns>The resulting QueryString</returns>
    public static QueryString FromUriComponent(string uriComponent)
    {
        if (string.IsNullOrEmpty(uriComponent))
        {
            return new QueryString(string.Empty);
        }
        return new QueryString(uriComponent);
    }

    /// <summary>
    /// Returns an QueryString given the query as from a Uri object. Relative Uri objects are not supported.
    /// </summary>
    /// <param name="uri">The Uri object</param>
    /// <returns>The resulting QueryString</returns>
    public static QueryString FromUriComponent(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        string queryValue = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
        if (!string.IsNullOrEmpty(queryValue))
        {
            queryValue = "?" + queryValue;
        }
        return new QueryString(queryValue);
    }

    /// <summary>
    /// Create a query string with a single given parameter name and value.
    /// </summary>
    /// <param name="name">The un-encoded parameter name</param>
    /// <param name="value">The un-encoded parameter value</param>
    /// <returns>The resulting QueryString</returns>
    public static QueryString Create(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!string.IsNullOrEmpty(value))
        {
            value = UrlEncoder.Default.Encode(value);
        }
        return new QueryString($"?{UrlEncoder.Default.Encode(name)}={value}");
    }

    /// <summary>
    /// Creates a query string composed from the given name value pairs.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>The resulting QueryString</returns>
    public static QueryString Create(IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var pair in parameters)
        {
            AppendKeyValuePair(builder, pair.Key, pair.Value, first);
            first = false;
        }

        return new QueryString(builder.ToString());
    }

    /// <summary>
    /// Creates a query string composed from the given name value pairs.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns>The resulting QueryString</returns>
    public static QueryString Create(IEnumerable<KeyValuePair<string, StringValues>> parameters)
    {
        var builder = new StringBuilder();
        var first = true;

        foreach (var pair in parameters)
        {
            // If nothing in this pair.Values, append null value and continue
            if (StringValues.IsNullOrEmpty(pair.Value))
            {
                AppendKeyValuePair(builder, pair.Key, null, first);
                first = false;
                continue;
            }
            // Otherwise, loop through values in pair.Value
            foreach (var value in pair.Value)
            {
                AppendKeyValuePair(builder, pair.Key, value, first);
                first = false;
            }
        }

        return new QueryString(builder.ToString());
    }

    /// <summary>
    /// Concatenates <paramref name="other"/> to the current query string.
    /// </summary>
    /// <param name="other">The <see cref="QueryString"/> to concatenate.</param>
    /// <returns>The concatenated <see cref="QueryString"/>.</returns>
    public QueryString Add(QueryString other)
    {
        if (!HasValue || Value.Equals("?", StringComparison.Ordinal))
        {
            return other;
        }
        if (!other.HasValue || other.Value.Equals("?", StringComparison.Ordinal))
        {
            return this;
        }

        // ?name1=value1 Add ?name2=value2 returns ?name1=value1&name2=value2
        return new QueryString(string.Concat(Value, "&", other.Value.AsSpan(1)));
    }

    /// <summary>
    /// Concatenates a query string with <paramref name="name"/> and <paramref name="value"/>
    /// to the current query string.
    /// </summary>
    /// <param name="name">The name of the query string to concatenate.</param>
    /// <param name="value">The value of the query string to concatenate.</param>
    /// <returns>The concatenated <see cref="QueryString"/>.</returns>
    public QueryString Add(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!HasValue || Value.Equals("?", StringComparison.Ordinal))
        {
            return Create(name, value);
        }

        var builder = new StringBuilder(Value);
        AppendKeyValuePair(builder, name, value, first: false);
        return new QueryString(builder.ToString());
    }

    /// <summary>
    /// Evalutes if the current query string is equal to <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="QueryString"/> to compare.</param>
    /// <returns><see langword="true"/> if the query strings are equal.</returns>
    public bool Equals(QueryString other)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Evaluates if the current query string is equal to an object <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">An object to compare.</param>
    /// <returns><see langword="true" /> if the query strings are equal.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return !HasValue;
        }
        return obj is QueryString query && Equals(query);
    }

    /// <summary>
    /// Gets a hash code for the value.
    /// </summary>
    /// <returns>The hash code as an <see cref="int"/>.</returns>
    public override int GetHashCode()
    {
        return (HasValue ? Value.GetHashCode() : 0);
    }

    /// <summary>
    /// Evaluates if one query string is equal to another.
    /// </summary>
    /// <param name="left">A <see cref="QueryString"/> instance.</param>
    /// <param name="right">A <see cref="QueryString"/> instance.</param>
    /// <returns><see langword="true" /> if the query strings are equal.</returns>
    public static bool operator ==(QueryString left, QueryString right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Evaluates if one query string is not equal to another.
    /// </summary>
    /// <param name="left">A <see cref="QueryString"/> instance.</param>
    /// <param name="right">A <see cref="QueryString"/> instance.</param>
    /// <returns><see langword="true" /> if the query strings are not equal.</returns>
    public static bool operator !=(QueryString left, QueryString right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Concatenates <paramref name="left"/> and <paramref name="right"/> into a single query string.
    /// </summary>
    /// <param name="left">A <see cref="QueryString"/> instance.</param>
    /// <param name="right">A <see cref="QueryString"/> instance.</param>
    /// <returns>The concatenated <see cref="QueryString"/>.</returns>
    public static QueryString operator +(QueryString left, QueryString right)
    {
        return left.Add(right);
    }

    private static void AppendKeyValuePair(StringBuilder builder, string key, string? value, bool first)
    {
        builder.Append(first ? '?' : '&');
        builder.Append(UrlEncoder.Default.Encode(key));
        builder.Append('=');
        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(UrlEncoder.Default.Encode(value));
        }
    }
}
