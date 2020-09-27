// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides correct handling for QueryString value when needed to reconstruct a request or redirect URI string
    /// </summary>
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
            return !string.IsNullOrEmpty(Value) ? Value!.Replace("#", "%23") : string.Empty;
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
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

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
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!string.IsNullOrEmpty(value))
            {
                value = UrlEncoder.Default.Encode(value);
            }
            return new QueryString($"?{UrlEncoder.Default.Encode(name)}={value}");
        }

        /// <summary>
        /// Creates a query string composed from the given name value pairs.
        /// </summary>
        /// <param name="parameters">A sequence of name and value pairs.</param>
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
        /// <param name="parameters">A sequence of name value pairs.</param>
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
        /// Adds the contents of <paramref name="other" /> to this <see cref="QueryString"/> instance.
        /// </summary>
        /// <param name="other">The <see cref="QueryString"/> to add.</param>
        /// <returns>
        /// The <paramref name="other" /> instance if this instance does not have a value. Otherwise, this instance
        /// after the copy operation.
        /// </returns>
        public QueryString Add(QueryString other)
        {
            if (!HasValue || Value!.Equals("?", StringComparison.Ordinal))
            {
                return other;
            }
            if (!other.HasValue || other.Value!.Equals("?", StringComparison.Ordinal))
            {
                return this;
            }

            // ?name1=value1 Add ?name2=value2 returns ?name1=value1&name2=value2
            return new QueryString(Value + "&" + other.Value.Substring(1));
        }

        /// <summary>
        /// Adds the name and value pair to the <see cref="QueryString"/> instance.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>The <see cref="QueryString"/> instance after the operation has completed.</returns>
        public QueryString Add(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!HasValue || Value!.Equals("?", StringComparison.Ordinal))
            {
                return Create(name, value);
            }

            var builder = new StringBuilder(Value);
            AppendKeyValuePair(builder, name, value, first: false);
            return new QueryString(builder.ToString());
        }

        /// <summary>
        /// Determines whether the specified <see cref="QueryString "/> is equal to the current <see cref="QueryString"/>.
        /// </summary>
        /// <param name="other">The <see cref="QueryString"/> to compare the current instance with.</param>
        /// <returns><see langword="true"/> if the specified value is equal to the current instance; otherwise, <see langword="false"/>.</returns>
        public bool Equals(QueryString other)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified value is equal to the current <see cref="QueryString"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare the current instance with.</param>
        /// <returns><see langword="true"/> if the specified value is equal to the current instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return !HasValue;
            }
            return obj is QueryString && Equals((QueryString)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (HasValue ? Value!.GetHashCode() : 0);
        }

        /// <summary>
        /// Determines if the two <see cref="QueryString"/> have the same value.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><see langword="true" /> if the two are the same, otherwise <see langword="false" />.</returns>
        public static bool operator ==(QueryString left, QueryString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines if the two <see cref="QueryString"/> have the different values.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><see langword="true" /> if the two are different, otherwise <see langword="false" />.</returns>
        public static bool operator !=(QueryString left, QueryString right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Adds the contents of the two query strings.
        /// </summary>
        /// <param name="left">The first instance to add.</param>
        /// <param name="right">The second instance to add.</param>
        /// <returns>The result of adding the two.</returns>
        public static QueryString operator +(QueryString left, QueryString right)
        {
            return left.Add(right);
        }

        private static void AppendKeyValuePair(StringBuilder builder, string key, string? value, bool first)
        {
            builder.Append(first ? "?" : "&");
            builder.Append(UrlEncoder.Default.Encode(key));
            builder.Append("=");
            if (!string.IsNullOrEmpty(value))
            {
                builder.Append(UrlEncoder.Default.Encode(value));
            }
        }
    }
}
