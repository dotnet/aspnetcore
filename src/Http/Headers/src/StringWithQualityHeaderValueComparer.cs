// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

/// <summary>
/// Implementation of <see cref="IComparer{T}"/> that can compare content negotiation header fields
/// based on their quality values (a.k.a q-values). This applies to values used in accept-charset,
/// accept-encoding, accept-language and related header fields with similar syntax rules. See
/// <see cref="MediaTypeHeaderValueComparer"/> for a comparer for media type
/// q-values.
/// </summary>
public class StringWithQualityHeaderValueComparer : IComparer<StringWithQualityHeaderValue>
{
    private static readonly StringSegment Any = new("*");

    private StringWithQualityHeaderValueComparer()
    {
    }

    /// <summary>
    /// Gets the default instance of <see cref="StringWithQualityHeaderValueComparer"/>.
    /// </summary>
    public static StringWithQualityHeaderValueComparer QualityComparer { get; } = new StringWithQualityHeaderValueComparer();

    /// <summary>
    /// Compares two <see cref="StringWithQualityHeaderValue"/> based on their quality value
    /// (a.k.a their "q-value").
    /// Values with identical q-values are considered equal (i.e the result is 0) with the exception of wild-card
    /// values (i.e. a value of "*") which are considered less than non-wild-card values. This allows to sort
    /// a sequence of <see cref="StringWithQualityHeaderValue"/> following their q-values ending up with any
    /// wild-cards at the end.
    /// </summary>
    /// <param name="stringWithQuality1">The first value to compare.</param>
    /// <param name="stringWithQuality2">The second value to compare</param>
    /// <returns>The result of the comparison.</returns>
    public int Compare(
        StringWithQualityHeaderValue? stringWithQuality1,
        StringWithQualityHeaderValue? stringWithQuality2)
    {
        ArgumentNullException.ThrowIfNull(stringWithQuality1);
        ArgumentNullException.ThrowIfNull(stringWithQuality2);

        var quality1 = stringWithQuality1.Quality ?? HeaderQuality.Match;
        var quality2 = stringWithQuality2.Quality ?? HeaderQuality.Match;
        var qualityDifference = quality1 - quality2;
        if (qualityDifference < 0)
        {
            return -1;
        }
        else if (qualityDifference > 0)
        {
            return 1;
        }

        if (!StringSegment.Equals(stringWithQuality1.Value, stringWithQuality2.Value, StringComparison.OrdinalIgnoreCase))
        {
            if (StringSegment.Equals(stringWithQuality1.Value, Any, StringComparison.Ordinal))
            {
                return -1;
            }
            else if (StringSegment.Equals(stringWithQuality2.Value, Any, StringComparison.Ordinal))
            {
                return 1;
            }
        }

        return 0;
    }
}
