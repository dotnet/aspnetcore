// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

/// <summary>
/// Immutable abstract representation of a span of text.  For example, in an error diagnostic that reports a
/// location, it could come from a parsed string, text from a tool editor buffer, etc.
/// </summary>
internal readonly struct TextSpan : IEquatable<TextSpan>, IComparable<TextSpan>
{
    /// <summary>
    /// Creates a TextSpan instance beginning with the position Start and having the Length
    /// specified with <paramref name="length" />.
    /// </summary>
    public TextSpan(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (start + length < start)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        Start = start;
        Length = length;
    }

    /// <summary>
    /// Start point of the span.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// End of the span.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Length of the span.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Determines whether or not the span is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Determines whether the position lies within the span.
    /// </summary>
    /// <param name="position">
    /// The position to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the position is greater than or equal to Start and strictly less
    /// than End, otherwise <c>false</c>.
    /// </returns>
    public bool Contains(int position)
    {
        return unchecked((uint)(position - Start) < (uint)Length);
    }

    /// <summary>
    /// Determines whether <paramref name="span"/> falls completely within this span.
    /// </summary>
    /// <param name="span">
    /// The span to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified span falls completely within this span, otherwise <c>false</c>.
    /// </returns>
    public bool Contains(TextSpan span)
    {
        return span.Start >= Start && span.End <= End;
    }

    /// <summary>
    /// Determines whether <paramref name="span"/> overlaps this span. Two spans are considered to overlap
    /// if they have positions in common and neither is empty. Empty spans do not overlap with any
    /// other span.
    /// </summary>
    /// <param name="span">
    /// The span to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the spans overlap, otherwise <c>false</c>.
    /// </returns>
    public bool OverlapsWith(TextSpan span)
    {
        var overlapStart = Math.Max(Start, span.Start);
        var overlapEnd = Math.Min(End, span.End);

        return overlapStart < overlapEnd;
    }

    /// <summary>
    /// Returns the overlap with the given span, or null if there is no overlap.
    /// </summary>
    /// <param name="span">
    /// The span to check.
    /// </param>
    /// <returns>
    /// The overlap of the spans, or null if the overlap is empty.
    /// </returns>
    public TextSpan? Overlap(TextSpan span)
    {
        var overlapStart = Math.Max(Start, span.Start);
        var overlapEnd = Math.Min(End, span.End);

        return overlapStart < overlapEnd
            ? FromBounds(overlapStart, overlapEnd)
            : (TextSpan?)null;
    }

    /// <summary>
    /// Determines whether <paramref name="span"/> intersects this span. Two spans are considered to
    /// intersect if they have positions in common or the end of one span
    /// coincides with the start of the other span.
    /// </summary>
    /// <param name="span">
    /// The span to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the spans intersect, otherwise <c>false</c>.
    /// </returns>
    public bool IntersectsWith(TextSpan span)
    {
        return span.Start <= End && span.End >= Start;
    }

    /// <summary>
    /// Determines whether <paramref name="position"/> intersects this span.
    /// A position is considered to intersect if it is between the start and
    /// end positions (inclusive) of this span.
    /// </summary>
    /// <param name="position">
    /// The position to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the position intersects, otherwise <c>false</c>.
    /// </returns>
    public bool IntersectsWith(int position)
    {
        return unchecked((uint)(position - Start) <= (uint)Length);
    }

    /// <summary>
    /// Returns the intersection with the given span, or null if there is no intersection.
    /// </summary>
    /// <param name="span">
    /// The span to check.
    /// </param>
    /// <returns>
    /// The intersection of the spans, or null if the intersection is empty.
    /// </returns>
    public TextSpan? Intersection(TextSpan span)
    {
        var intersectStart = Math.Max(Start, span.Start);
        var intersectEnd = Math.Min(End, span.End);

        return intersectStart <= intersectEnd
            ? FromBounds(intersectStart, intersectEnd)
            : (TextSpan?)null;
    }

    /// <summary>
    /// Creates a new <see cref="TextSpan"/> from <paramref name="start" /> and <paramref
    /// name="end"/> positions as opposed to a position and length.
    ///
    /// The returned TextSpan contains the range with <paramref name="start"/> inclusive,
    /// and <paramref name="end"/> exclusive.
    /// </summary>
    public static TextSpan FromBounds(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "start must not be negative");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "end must not be less than start");
        }

        return new TextSpan(start, end - start);
    }

    /// <summary>
    /// Determines if two instances of <see cref="TextSpan"/> are the same.
    /// </summary>
    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines if two instances of <see cref="TextSpan"/> are different.
    /// </summary>
    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Determines if current instance of <see cref="TextSpan"/> is equal to another.
    /// </summary>
    public bool Equals(TextSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    /// <summary>
    /// Determines if current instance of <see cref="TextSpan"/> is equal to another.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is TextSpan && Equals((TextSpan)obj);
    }

    /// <summary>
    /// Produces a hash code for <see cref="TextSpan"/>.
    /// </summary>
    public override int GetHashCode()
    {
        var combiner = HashCodeCombiner.Start();
        combiner.Add(Start);
        combiner.Add(Length);

        return combiner.CombinedHash;
    }

    /// <summary>
    /// Provides a string representation for <see cref="TextSpan"/>.
    /// </summary>
    public override string ToString()
    {
        return $"[{Start}..{End})";
    }

    /// <summary>
    /// Compares current instance of <see cref="TextSpan"/> with another.
    /// </summary>
    public int CompareTo(TextSpan other)
    {
        var diff = Start - other.Start;
        if (diff != 0)
        {
            return diff;
        }

        return Length - other.Length;
    }
}
