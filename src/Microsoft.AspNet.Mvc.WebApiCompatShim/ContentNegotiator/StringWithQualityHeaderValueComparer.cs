// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// Implementation of <see cref="IComparer{T}"/> that can compare content negotiation header fields
    /// based on their quality values (a.k.a q-values). This applies to values used in accept-charset,
    /// accept-encoding, accept-language and related header fields with similar syntax rules. See
    /// <see cref="MediaTypeWithQualityHeaderValueComparer"/> for a comparer for media type
    /// q-values.
    /// </summary>
    internal class StringWithQualityHeaderValueComparer : IComparer<StringWithQualityHeaderValue>
    {
        private static readonly StringWithQualityHeaderValueComparer _qualityComparer =
            new StringWithQualityHeaderValueComparer();

        private StringWithQualityHeaderValueComparer()
        {
        }

        public static StringWithQualityHeaderValueComparer QualityComparer
        {
            get { return _qualityComparer; }
        }

        /// <summary>
        /// Compares two <see cref="StringWithQualityHeaderValue"/> based on their quality value (a.k.a their
        /// "q-value"). Values with identical q-values are considered equal (i.e the result is 0) with the exception of
        /// wild-card values (i.e. a value of "*") which are considered less than non-wild-card values. This allows to
        /// sort a sequence of <see cref="StringWithQualityHeaderValue"/> following their q-values ending up with any
        /// wild-cards at the end.
        /// </summary>
        /// <param name="stringWithQuality1">The first value to compare.</param>
        /// <param name="stringWithQuality2">The second value to compare</param>
        /// <returns>The result of the comparison.</returns>
        public int Compare(StringWithQualityHeaderValue stringWithQuality1,
                           StringWithQualityHeaderValue stringWithQuality2)
        {
            Debug.Assert(stringWithQuality1 != null);
            Debug.Assert(stringWithQuality2 != null);

            var quality1 = stringWithQuality1.Quality ?? FormattingUtilities.Match;
            var quality2 = stringWithQuality2.Quality ?? FormattingUtilities.Match;
            var qualityDifference = quality1 - quality2;
            if (qualityDifference < 0)
            {
                return -1;
            }
            else if (qualityDifference > 0)
            {
                return 1;
            }

            if (!string.Equals(stringWithQuality1.Value, stringWithQuality2.Value, StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(stringWithQuality1.Value, "*", StringComparison.Ordinal))
                {
                    return -1;
                }
                else if (String.Equals(stringWithQuality2.Value, "*", StringComparison.Ordinal))
                {
                    return 1;
                }
            }

            return 0;
        }
    }
}
#endif