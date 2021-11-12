// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor;

/// <summary>
/// An optimized representation of a substring.
/// <p>
/// We're using our own copy of StringSegment rather than using Span or StringSegment from M.Extensions.Primitives
/// to avoid cross-compiling this project to support source build and to avoid adding new dependencies to the IDE.
/// </p>
/// </summary>
internal readonly struct StringSegment : IEquatable<StringSegment>, IEquatable<string>
{
    /// <summary>
    /// A <see cref="StringSegment"/> for <see cref="string.Empty"/>.
    /// </summary>
    public static readonly StringSegment Empty = string.Empty;

    public StringSegment(string buffer)
    {
        Buffer = buffer;
        Offset = 0;
        Length = buffer?.Length ?? 0;
    }

    public StringSegment(string buffer, int offset)
    {
        Debug.Assert(buffer != null);
        Buffer = buffer;
        Offset = offset;
        Length = buffer.Length - offset;
    }

    /// <summary>
    /// Initializes an instance of the <see cref="StringSegment"/> struct.
    /// </summary>
    /// <param name="buffer">The original <see cref="string"/> used as buffer.</param>
    /// <param name="offset">The offset of the segment within the <paramref name="buffer"/>.</param>
    /// <param name="length">The length of the segment.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSegment(string buffer, int offset, int length)
    {
        Debug.Assert(buffer != null);
        Buffer = buffer;
        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Gets the <see cref="string"/> buffer for this <see cref="StringSegment"/>.
    /// </summary>
    public string Buffer { get; }

    /// <summary>
    /// Gets the offset within the buffer for this <see cref="StringSegment"/>.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the length of this <see cref="StringSegment"/>.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the value of this segment as a <see cref="string"/>.
    /// </summary>
    public string Value => HasValue ? Buffer.Substring(Offset, Length) : null;

    public bool IsEmpty => Length == 0;

    public bool HasValue => Buffer != null;

    /// <summary>
    /// Gets the <see cref="char"/> at a specified position in the current <see cref="StringSegment"/>.
    /// </summary>
    /// <param name="index">The offset into the <see cref="StringSegment"/></param>
    /// <returns>The <see cref="char"/> at a specified position.</returns>
    public char this[int index]
    {
        get
        {
            return Buffer[Offset + index];
        }
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        return obj is StringSegment segment && Equals(segment);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><code>true</code> if the current object is equal to the other parameter; otherwise, <code>false</code>.</returns>
    public bool Equals(StringSegment other)
    {
        return Equals(other, StringComparison.Ordinal);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns><code>true</code> if the current object is equal to the other parameter; otherwise, <code>false</code>.</returns>
    public bool Equals(StringSegment other, StringComparison comparisonType)
    {
        var textLength = other.Length;
        if (Length != textLength)
        {
            return false;
        }

        return string.Compare(Buffer, Offset, other.Buffer, other.Offset, textLength, comparisonType) == 0;
    }

    // This handles StringSegment.Equals(string, StringSegment, StringComparison) and StringSegment.Equals(StringSegment, string, StringComparison)
    // via the implicit type converter
    /// <summary>
    /// Determines whether two specified StringSegment objects have the same value. A parameter specifies the culture, case, and
    /// sort rules used in the comparison.
    /// </summary>
    /// <param name="a">The first StringSegment to compare.</param>
    /// <param name="b">The second StringSegment to compare.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the comparison.</param>
    /// <returns><code>true</code> if the objects are equal; otherwise, <code>false</code>.</returns>
    public static bool Equals(StringSegment a, StringSegment b, StringComparison comparisonType)
    {
        return a.Equals(b, comparisonType);
    }

    /// <summary>
    /// Checks if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>.
    /// </summary>
    /// <param name="text">The <see cref="string"/> to compare with the current <see cref="StringSegment"/>.</param>
    /// <returns><code>true</code> if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
    public bool Equals(string text)
    {
        return Equals(text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>.
    /// </summary>
    /// <param name="text">The <see cref="string"/> to compare with the current <see cref="StringSegment"/>.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns><code>true</code> if the specified <see cref="string"/> is equal to the current <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
    public bool Equals(string text, StringComparison comparisonType)
        => Equals(new StringSegment(text), comparisonType);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (!HasValue)
        {
            return 0;
        }

        if (Offset == 0 && Length == Buffer.Length)
        {
            return Buffer.GetHashCode();
        }

        return Value.GetHashCode();
    }

    /// <summary>
    /// Checks if two specified <see cref="StringSegment"/> have the same value.
    /// </summary>
    /// <param name="left">The first <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
    /// <param name="right">The second <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
    /// <returns><code>true</code> if the value of <paramref name="left"/> is the same as the value of <paramref name="right"/>; otherwise, <code>false</code>.</returns>
    public static bool operator ==(StringSegment left, StringSegment right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two specified <see cref="StringSegment"/> have different values.
    /// </summary>
    /// <param name="left">The first <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
    /// <param name="right">The second <see cref="StringSegment"/> to compare, or <code>null</code>.</param>
    /// <returns><code>true</code> if the value of <paramref name="left"/> is different from the value of <paramref name="right"/>; otherwise, <code>false</code>.</returns>
    public static bool operator !=(StringSegment left, StringSegment right)
    {
        return !left.Equals(right);
    }

    // PERF: Do NOT add a implicit converter from StringSegment to String. That would negate most of the perf safety.
    /// <summary>
    /// Creates a new <see cref="StringSegment"/> from the given <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert to a <see cref="StringSegment"/></param>
    public static implicit operator StringSegment(string value)
    {
        return new StringSegment(value);
    }

    /// <summary>
    /// Checks if the beginning of this <see cref="StringSegment"/> matches the specified <see cref="string"/> when compared using the specified <paramref name="comparisonType"/>.
    /// </summary>
    /// <param name="text">The <see cref="string"/>to compare.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns><code>true</code> if <paramref name="text"/> matches the beginning of this <see cref="StringSegment"/>; otherwise, <code>false</code>.</returns>
    public bool StartsWith(string text, StringComparison comparisonType)
        => StartsWith(new StringSegment(text), comparisonType);

    public bool StartsWith(StringSegment text, StringComparison comparisonType)
    {
        var textLength = text.Length;
        if (!HasValue || Length < text.Length)
        {
            return false;
        }

        return string.Compare(Buffer, Offset, text.Buffer, text.Offset, textLength, comparisonType) == 0;

    }

    /// <summary>
    /// Retrieves a <see cref="StringSegment"/> that represents a substring from this <see cref="StringSegment"/>.
    /// The <see cref="StringSegment"/> starts at the position specified by <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">The zero-based starting character position of a substring in this <see cref="StringSegment"/>.</param>
    /// <returns>A <see cref="StringSegment"/> that begins at <paramref name="offset"/> in this <see cref="StringSegment"/>
    /// whose length is the remainder.</returns>
    public StringSegment Subsegment(int offset)
    {
        return Subsegment(offset, Length - offset);
    }

    /// <summary>
    /// Retrieves a <see cref="StringSegment"/> that represents a substring from this <see cref="StringSegment"/>.
    /// The <see cref="StringSegment"/> starts at the position specified by <paramref name="offset"/> and has the specified <paramref name="length"/>.
    /// </summary>
    /// <param name="offset">The zero-based starting character position of a substring in this <see cref="StringSegment"/>.</param>
    /// <param name="length">The number of characters in the substring.</param>
    /// <returns>A <see cref="StringSegment"/> that is equivalent to the substring of length <paramref name="length"/> that begins at <paramref name="offset"/> in this <see cref="StringSegment"/></returns>
    public StringSegment Subsegment(int offset, int length)
    {
        return new StringSegment(Buffer, Offset + offset, length);
    }

    /// <summary>
    /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
    /// The search starts at <paramref name="start"/> and examines a specified number of <paramref name="count"/> character positions.
    /// </summary>
    /// <param name="c">The Unicode character to seek.</param>
    /// <param name="start">The zero-based index position at which the search starts. </param>
    /// <param name="count">The number of characters to examine.</param>
    /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
    public int IndexOf(char c, int start, int count)
    {
        var index = Buffer.IndexOf(c, start + Offset, count);
        if (index != -1)
        {
            return index - Offset;
        }
        else
        {
            return index;
        }
    }

    /// <summary>
    /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
    /// The search starts at <paramref name="start"/>.
    /// </summary>
    /// <param name="c">The Unicode character to seek.</param>
    /// <param name="start">The zero-based index position at which the search starts. </param>
    /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
    public int IndexOf(char c, int start)
    {
        return IndexOf(c, start, Length - start);
    }

    /// <summary>
    /// Gets the zero-based index of the first occurrence of the character <paramref name="c"/> in this <see cref="StringSegment"/>.
    /// </summary>
    /// <param name="c">The Unicode character to seek.</param>
    /// <returns>The zero-based index position of <paramref name="c"/> from the beginning of the <see cref="StringSegment"/> if that character is found, or -1 if it is not.</returns>
    public int IndexOf(char c)
    {
        return IndexOf(c, 0, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        var index = -1;

        if (HasValue)
        {
            index = Buffer.IndexOfAny(anyOf, Offset + startIndex, count);
            if (index != -1)
            {
                index -= Offset;
            }
        }

        return index;
    }

    public int IndexOfAny(char[] anyOf, int startIndex)
    {
        return IndexOfAny(anyOf, startIndex, Length - startIndex);
    }

    public int IndexOfAny(char[] anyOf)
    {
        return IndexOfAny(anyOf, 0, Length);
    }

    /// <summary>
    /// Indicates whether the specified StringSegment is null or an Empty string.
    /// </summary>
    /// <param name="value">The StringSegment to test.</param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(StringSegment value)
    {
        return !value.HasValue || value.Length == 0;
    }

    /// <summary>
    /// Returns the <see cref="string"/> represented by this <see cref="StringSegment"/> or <code>String.Empty</code> if the <see cref="StringSegment"/> does not contain a value.
    /// </summary>
    /// <returns>The <see cref="string"/> represented by this <see cref="StringSegment"/> or <code>String.Empty</code> if the <see cref="StringSegment"/> does not contain a value.</returns>
    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
