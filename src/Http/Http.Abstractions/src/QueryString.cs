// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides correct handling for QueryString value when needed to reconstruct a request or redirect URI string
    /// </summary>
    public struct QueryString : IEquatable<QueryString>
    {
        /// <summary>
        /// Represents the empty query string. This field is read-only.
        /// </summary>
        public static readonly QueryString Empty = new QueryString(string.Empty);

        private readonly string _value;

        /// <summary>
        /// Initialize the query string with a given value. This value must be in escaped and delimited format with
        /// a leading '?' character. 
        /// </summary>
        /// <param name="value">The query string to be assigned to the Value property.</param>
        public QueryString(string value)
        {
            if (!string.IsNullOrEmpty(value) && value[0] != '?')
            {
                throw new ArgumentException("The leading '?' must be included for a non-empty query.", nameof(value));
            }
            _value = value;
        }

        /// <summary>
        /// The escaped query string with the leading '?' character
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True if the query string is not empty
        /// </summary>
        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(_value); }
        }

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
            return HasValue ? _value.Replace("#", "%23") : string.Empty;
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
        /// <param name="parameters"></param>
        /// <returns>The resulting QueryString</returns>
        public static QueryString Create(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();
            bool first = true;
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
            bool first = true;

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
            return new QueryString(_value + "&" + other.Value.Substring(1));
        }

        public QueryString Add(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!HasValue || Value.Equals("?", StringComparison.Ordinal))
            {
                return Create(name, value);
            }

            var builder = new StringBuilder(Value);
            AppendKeyValuePair(builder, name, value, first: false);
            return new QueryString(builder.ToString());
        }

        public bool Equals(QueryString other)
        {
            if (!HasValue && !other.HasValue)
            {
                return true;
            }
            return string.Equals(_value, other._value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return !HasValue;
            }
            return obj is QueryString && Equals((QueryString)obj);
        }

        public override int GetHashCode()
        {
            return (HasValue ? _value.GetHashCode() : 0);
        }

        public static bool operator ==(QueryString left, QueryString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QueryString left, QueryString right)
        {
            return !left.Equals(right);
        }

        public static QueryString operator +(QueryString left, QueryString right)
        {
            return left.Add(right);
        }

        private static void AppendKeyValuePair(StringBuilder builder, string key, string value, bool first)
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
