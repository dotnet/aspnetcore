// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides correct escaping for Path and PathBase values when needed to reconstruct a request or redirect URI string
/// </summary>
[TypeConverter(typeof(PathStringConverter))]
[DebuggerDisplay("{Value}")]
public readonly struct PathString : IEquatable<PathString>
{
    private static readonly SearchValues<char> s_validPathChars =
        SearchValues.Create("!$&'()*+,-./0123456789:;=@ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~");

    internal const int StackAllocThreshold = 128;

    /// <summary>
    /// Represents the empty path. This field is read-only.
    /// </summary>
    public static readonly PathString Empty = new(string.Empty);

    /// <summary>
    /// Initialize the path string with a given value. This value must be in unescaped format. Use
    /// PathString.FromUriComponent(value) if you have a path value which is in an escaped format.
    /// </summary>
    /// <param name="value">The unescaped path to be assigned to the Value property.</param>
    public PathString(string? value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '/')
        {
            throw new ArgumentException(Resources.FormatException_PathMustStartWithSlash(nameof(value)), nameof(value));
        }
        Value = value;
    }

    /// <summary>
    /// The unescaped path value
    /// </summary>
    public string? Value { get; }

    /// <summary>
    /// True if the path is not empty
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue
    {
        get { return !string.IsNullOrEmpty(Value); }
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
    public string ToUriComponent()
    {
        var value = Value;

        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var indexOfInvalidChar = value.AsSpan().IndexOfAnyExcept(s_validPathChars);

        return indexOfInvalidChar < 0
            ? value
            : ToEscapedUriComponent(value, indexOfInvalidChar);
    }

    private static string ToEscapedUriComponent(string value, int i)
    {
        StringBuilder? buffer = null;

        var start = 0;
        var count = i;
        var requiresEscaping = false;

        while ((uint)i < (uint)value.Length)
        {
            var isPercentEncodedChar = false;
            if (s_validPathChars.Contains(value[i]) || (isPercentEncodedChar = Uri.IsHexEncoding(value, i)))
            {
                if (requiresEscaping)
                {
                    // the current segment requires escape
                    buffer ??= new StringBuilder(value.Length * 3);
                    buffer.Append(Uri.EscapeDataString(value.AsSpan(start, count)));

                    requiresEscaping = false;
                    start = i;
                    count = 0;
                }

                if (isPercentEncodedChar)
                {
                    count += 3;
                    i += 3;
                }
                else
                {
                    // We just saw a character we don't want to escape. It's likely there are more, do a vectorized search.
                    var charsToSkip = value.AsSpan(i).IndexOfAnyExcept(s_validPathChars);

                    if (charsToSkip < 0)
                    {
                        // Only valid characters remain
                        count += value.Length - i;
                        break;
                    }

                    count += charsToSkip;
                    i += charsToSkip;
                }
            }
            else
            {
                if (!requiresEscaping)
                {
                    // the current segment doesn't require escape
                    buffer ??= new StringBuilder(value.Length * 3);
                    buffer.Append(value, start, count);

                    requiresEscaping = true;
                    start = i;
                    count = 0;
                }

                count++;
                i++;
            }
        }

        if (count == value.Length && !requiresEscaping)
        {
            return value;
        }
        else
        {
            Debug.Assert(count > 0);
            Debug.Assert(buffer is not null);

            if (requiresEscaping)
            {
                buffer.Append(Uri.EscapeDataString(value.AsSpan(start, count)));
            }
            else
            {
                buffer.Append(value, start, count);
            }

            return buffer.ToString();
        }
    }

    /// <summary>
    /// Returns an PathString given the path as it is escaped in the URI format. The string MUST NOT contain any
    /// value that is not a path.
    /// </summary>
    /// <param name="uriComponent">The escaped path as it appears in the URI format.</param>
    /// <returns>The resulting PathString</returns>
    public static PathString FromUriComponent(string uriComponent)
    {
        int position = uriComponent.IndexOf('%');
        if (position == -1)
        {
            return new PathString(uriComponent);
        }
        Span<char> pathBuffer = uriComponent.Length <= StackAllocThreshold ? stackalloc char[StackAllocThreshold] : new char[uriComponent.Length];
        uriComponent.CopyTo(pathBuffer);
        var length = UrlDecoder.DecodeInPlace(pathBuffer.Slice(position, uriComponent.Length - position));
        pathBuffer = pathBuffer.Slice(0, position + length);
        return new PathString(pathBuffer.ToString());
    }

    /// <summary>
    /// Returns an PathString given the path as from a Uri object. Relative Uri objects are not supported.
    /// </summary>
    /// <param name="uri">The Uri object</param>
    /// <returns>The resulting PathString</returns>
    public static PathString FromUriComponent(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var uriComponent = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        Span<char> pathBuffer = uriComponent.Length < StackAllocThreshold ? stackalloc char[StackAllocThreshold] : new char[uriComponent.Length + 1];
        pathBuffer[0] = '/';
        var length = UrlDecoder.DecodeRequestLine(uriComponent.AsSpan(), pathBuffer.Slice(1));
        pathBuffer = pathBuffer.Slice(0, length + 1);
        return new PathString(pathBuffer.ToString());
    }

    /// <summary>
    /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/>.
    /// </summary>
    /// <param name="other">The <see cref="PathString"/> to compare.</param>
    /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
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
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
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
    /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> and returns
    /// the remaining segments.
    /// </summary>
    /// <param name="other">The <see cref="PathString"/> to compare.</param>
    /// <param name="remaining">The remaining segments after the match.</param>
    /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
    public bool StartsWithSegments(PathString other, out PathString remaining)
    {
        return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out remaining);
    }

    /// <summary>
    /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> when compared
    /// using the specified comparison option and returns the remaining segments.
    /// </summary>
    /// <param name="other">The <see cref="PathString"/> to compare.</param>
    /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="PathString"/> and value are compared.</param>
    /// <param name="remaining">The remaining segments after the match.</param>
    /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
    public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString remaining)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, comparisonType))
        {
            if (value1.Length == value2.Length || value1[value2.Length] == '/')
            {
                remaining = new PathString(value1[value2.Length..]);
                return true;
            }
        }
        remaining = Empty;
        return false;
    }

    /// <summary>
    /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> and returns
    /// the matched and remaining segments.
    /// </summary>
    /// <param name="other">The <see cref="PathString"/> to compare.</param>
    /// <param name="matched">The matched segments with the original casing in the source value.</param>
    /// <param name="remaining">The remaining segments after the match.</param>
    /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
    public bool StartsWithSegments(PathString other, out PathString matched, out PathString remaining)
    {
        return StartsWithSegments(other, StringComparison.OrdinalIgnoreCase, out matched, out remaining);
    }

    /// <summary>
    /// Determines whether the beginning of this <see cref="PathString"/> instance matches the specified <see cref="PathString"/> when compared
    /// using the specified comparison option and returns the matched and remaining segments.
    /// </summary>
    /// <param name="other">The <see cref="PathString"/> to compare.</param>
    /// <param name="comparisonType">One of the enumeration values that determines how this <see cref="PathString"/> and value are compared.</param>
    /// <param name="matched">The matched segments with the original casing in the source value.</param>
    /// <param name="remaining">The remaining segments after the match.</param>
    /// <returns>true if value matches the beginning of this string; otherwise, false.</returns>
    /// <remarks>
    /// When the <paramref name="other"/> parameter contains a trailing slash, the <see cref="PathString"/> being checked
    /// must either exactly match or include a trailing slash. For instance, for a <see cref="PathString"/> of "/a/b",
    /// this method will return <c>true</c> for "/a", but will return <c>false</c> for "/a/".
    /// Whereas, a <see cref="PathString"/> of "/a//b/" will return <c>true</c> when compared with "/a/".
    /// </remarks>
    public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString matched, out PathString remaining)
    {
        var value1 = Value ?? string.Empty;
        var value2 = other.Value ?? string.Empty;
        if (value1.StartsWith(value2, comparisonType))
        {
            if (value1.Length == value2.Length || value1[value2.Length] == '/')
            {
                matched = new PathString(value1.Substring(0, value2.Length));
                remaining = new PathString(value1[value2.Length..]);
                return true;
            }
        }
        remaining = Empty;
        matched = Empty;
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
            Value[^1] == '/')
        {
            // If the path string has a trailing slash and the other string has a leading slash, we need
            // to trim one of them.
            var combined = string.Concat(Value.AsSpan(), other.Value.AsSpan(1));
            return new PathString(combined);
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
        return Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compares this PathString value to another value using a specific StringComparison type
    /// </summary>
    /// <param name="other">The second PathString for comparison</param>
    /// <param name="comparisonType">The StringComparison type to use</param>
    /// <returns>True if both PathString values are equal</returns>
    public bool Equals(PathString other, StringComparison comparisonType)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }
        return string.Equals(Value, other.Value, comparisonType);
    }

    /// <summary>
    /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
    /// </summary>
    /// <param name="obj">The second PathString for comparison.</param>
    /// <returns>True if both PathString values are equal</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return !HasValue;
        }
        return obj is PathString pathString && Equals(pathString);
    }

    /// <summary>
    /// Returns the hash code for the PathString value. The hash code is provided by the OrdinalIgnoreCase implementation.
    /// </summary>
    /// <returns>The hash code</returns>
    public override int GetHashCode()
    {
        return (HasValue ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : 0);
    }

    /// <summary>
    /// Operator call through to Equals
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>True if both PathString values are equal</returns>
    public static bool operator ==(PathString left, PathString right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Operator call through to Equals
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>True if both PathString values are not equal</returns>
    public static bool operator !=(PathString left, PathString right)
    {
        return !left.Equals(right);
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
    public static string operator +(PathString left, string? right)
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
    public static implicit operator PathString(string? s)
        => ConvertFromString(s);

    /// <summary>
    /// Implicitly calls ToString().
    /// </summary>
    /// <param name="path"></param>
    public static implicit operator string(PathString path)
        => path.ToString();

    internal static PathString ConvertFromString(string? s)
        => string.IsNullOrEmpty(s) ? new PathString(s) : FromUriComponent(s);
}

internal sealed class PathStringConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string @string
        ? PathString.ConvertFromString(@string)
        : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context,
       CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);

        return destinationType == typeof(string)
            ? value?.ToString() ?? string.Empty
            : base.ConvertTo(context, culture, value, destinationType);
    }
}
