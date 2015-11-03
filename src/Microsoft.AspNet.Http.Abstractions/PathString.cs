// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Provides correct escaping for Path and PathBase values when needed to reconstruct a request or redirect URI string
    /// </summary>
    public struct PathString : IEquatable<PathString>
    {
        private static readonly char[] splitChar = { '/' };

        /// <summary>
        /// Represents the empty path. This field is read-only.
        /// </summary>
        public static readonly PathString Empty = new PathString(string.Empty);

        private readonly string _value;

        /// <summary>
        /// Initalize the path string with a given value. This value must be in unescaped format. Use
        /// PathString.FromUriComponent(value) if you have a path value which is in an escaped format.
        /// </summary>
        /// <param name="value">The unescaped path to be assigned to the Value property.</param>
        public PathString(string value)
        {
            if (!string.IsNullOrEmpty(value) && value[0] != '/')
            {
                throw new ArgumentException(""/*Resources.Exception_PathMustStartWithSlash*/, nameof(value));
            }
            _value = value;
        }

        /// <summary>
        /// The unescaped path value
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True if the path is not empty
        /// </summary>
        public bool HasValue
        {
            get { return !string.IsNullOrEmpty(_value); }
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation. 
        /// </summary>
        /// <returns>The escaped path value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "Purpose of the method is to return a string")]
        public string ToUriComponent()
        {
            // TODO: Measure the cost of this escaping and consider optimizing.
            if (!HasValue)
            {
                return string.Empty;
            }
            var values = _value.Split(splitChar);
            var changed = false;
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                values[i] = UrlEncoder.Default.Encode(value);

                if (!changed && value != values[i])
                {
                    changed = true;
                }
            }

            return changed ? string.Join("/", values) : _value;
        }

        /// <summary>
        /// Returns an PathString given the path as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a path.
        /// </summary>
        /// <param name="uriComponent">The escaped path as it appears in the URI format.</param>
        /// <returns>The resulting PathString</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "Requirements not compatible with URI processing")]
        public static PathString FromUriComponent(string uriComponent)
        {
            // REVIEW: what is the exactly correct thing to do?
            return new PathString(Uri.UnescapeDataString(uriComponent));
        }

        /// <summary>
        /// Returns an PathString given the path as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            // REVIEW: what is the exactly correct thing to do?
            return new PathString("/" + uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/>.
        /// </summary>
        /// <param name="other">The <see cref="PathString"/> to compare.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other)
        {
            return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> when compared
        /// using the specified comparison option.
        /// </summary>
        /// <param name="other">The <see cref="PathString"/> to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="PathString"/> and value are compared.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        public bool StartsWithSegments(PathString other, StringComparison comparisonType)
        {
            var value1 = Value ?? string.Empty;
            var value2 = other.Value ?? string.Empty;
            if (value1.StartsWith(value2, comparisonType))
            {
                return value1.Length == value2.Length || value1[value2.Length] == '/';
            }
            return false;
        }

        /// <summary>
        /// Determines whether the beginning of this PathString instance matches the specified <see cref="PathString"/> when compared
        /// using the specified comparison option and returns the remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="PathString"/> to compare.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "Secondary information needed after boolean result obtained")]
        public bool StartsWithSegments(PathString other, out PathString remaining)
        {
            return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out remaining);
        }

        /// <summary>
        /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> and returns
        /// the remaining segments.
        /// </summary>
        /// <param name="other">The <see cref="PathString"/> to compare.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="PathString"/> and value are compared.</param>
        /// <param name="remaining">The remaining segments after the match.</param>
        /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#", Justification = "Secondary information needed after boolean result obtained")]
        public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString remaining)
        {
            var value1 = Value ?? string.Empty;
            var value2 = other.Value ?? string.Empty;
            if (value1.StartsWith(value2, comparisonType))
            {
                if (value1.Length == value2.Length || value1[value2.Length] == '/')
                {
                    remaining = new PathString(value1.Substring(value2.Length));
                    return true;
                }
            }
            remaining = Empty;
            return false;
        }

        /// <summary>
        /// Adds two PathString instances into a combined PathString value. 
        /// </summary>
        /// <returns>The combined PathString value</returns>
        public PathString Add(PathString other)
        {
            if (HasValue &&
                other.HasValue &&
                Value[Value.Length - 1] == '/')
            {
                // If the path string has a trailing slash and the other string has a leading slash, we need
                // to trim one of them.
                return new PathString(Value + other.Value.Substring(1));
            }

            return new PathString(Value + other.Value);
        }

        /// <summary>
        /// Combines a PathString and QueryString into the joined URI formatted string value. 
        /// </summary>
        /// <returns>The joined URI formatted string value</returns>
        public string Add(QueryString other)
        {
            return ToUriComponent() + other.ToUriComponent();
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="other">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other)
        {
            return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares this PathString value to another value using a specific StringComparison type
        /// </summary>
        /// <param name="other">The second PathString for comparison</param>
        /// <param name="comparisonType">The StringComparison type to use</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other, StringComparison comparisonType)
        {
            return string.Equals(_value, other._value, comparisonType);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="obj">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is PathString && Equals((PathString)obj, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the hash code for the PathString value. The hash code is provided by the OrdinalIgnoreCase implementation.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return (_value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are equal</returns>
        public static bool operator ==(PathString left, PathString right)
        {
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are not equal</returns>
        public static bool operator !=(PathString left, PathString right)
        {
            return !left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(string left, PathString right)
        {
            // This overload exists to prevent the implicit string<->PathString converter from
            // trying to call the PathString+PathString operator for things that are not path strings.
            return string.Concat(left, right.ToString());
        }

        /// <summary>
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The ToString combination of both values</returns>
        public static string operator +(PathString left, string right)
        {
            // This overload exists to prevent the implicit string<->PathString converter from
            // trying to call the PathString+PathString operator for things that are not path strings.
            return string.Concat(left.ToString(), right);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static PathString operator +(PathString left, PathString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static string operator +(PathString left, QueryString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Implicitly creates a new PathString from the given string.
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator PathString(string s)
        {
            return new PathString(s);
        }

        /// <summary>
        /// Implicitly calls ToString().
        /// </summary>
        /// <param name="path"></param>
        public static implicit operator string (PathString path)
        {
            return path.ToString();
        }
    }
}
