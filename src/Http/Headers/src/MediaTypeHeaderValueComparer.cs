// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Net.Http.Headers
{
    /// <summary>
    /// Implementation of <see cref="IComparer{T}"/> that can compare accept media type header fields
    /// based on their quality values (a.k.a q-values).
    /// </summary>
    public class MediaTypeHeaderValueComparer : IComparer<MediaTypeHeaderValue>
    {
        private static readonly MediaTypeHeaderValueComparer _mediaTypeComparer =
                                                                    new MediaTypeHeaderValueComparer();

        private MediaTypeHeaderValueComparer()
        {
        }

        public static MediaTypeHeaderValueComparer QualityComparer
        {
            get { return _mediaTypeComparer; }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Performs comparisons based on the arguments' quality values
        /// (aka their "q-value"). Values with identical q-values are considered equal (i.e. the result is 0)
        /// with the exception that suffixed subtype wildcards are considered less than subtype wildcards, subtype wildcards
        /// are considered less than specific media types and full wildcards are considered less than
        /// subtype wildcards. This allows callers to sort a sequence of <see cref="MediaTypeHeaderValue"/> following
        /// their q-values in the order of specific media types, subtype wildcards, and last any full wildcards.
        /// </remarks>
        /// <example>
        /// If we had a list of media types (comma separated): { text/*;q=0.8, text/*+json;q=0.8, */*;q=1, */*;q=0.8, text/plain;q=0.8 }
        /// Sorting them using Compare would return: { */*;q=0.8, text/*;q=0.8, text/*+json;q=0.8, text/plain;q=0.8, */*;q=1 }
        /// </example>
        public int Compare(MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2)
        {
            if (object.ReferenceEquals(mediaType1, mediaType2))
            {
                return 0;
            }

            var returnValue = CompareBasedOnQualityFactor(mediaType1, mediaType2);

            if (returnValue == 0)
            {
                if (!mediaType1.Type.Equals(mediaType2.Type, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType1.MatchesAllTypes)
                    {
                        return -1;
                    }
                    else if (mediaType2.MatchesAllTypes)
                    {
                        return 1;
                    }
                    else if (mediaType1.MatchesAllSubTypes && !mediaType2.MatchesAllSubTypes)
                    {
                        return -1;
                    }
                    else if (!mediaType1.MatchesAllSubTypes && mediaType2.MatchesAllSubTypes)
                    {
                        return 1;
                    }
                    else if (mediaType1.MatchesAllSubTypesWithoutSuffix && !mediaType2.MatchesAllSubTypesWithoutSuffix)
                    {
                        return -1;
                    }
                    else if (!mediaType1.MatchesAllSubTypesWithoutSuffix && mediaType2.MatchesAllSubTypesWithoutSuffix)
                    {
                        return 1;
                    }
                }
                else if (!mediaType1.SubType.Equals(mediaType2.SubType, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType1.MatchesAllSubTypes)
                    {
                        return -1;
                    }
                    else if (mediaType2.MatchesAllSubTypes)
                    {
                        return 1;
                    }
                    else if (mediaType1.MatchesAllSubTypesWithoutSuffix && !mediaType2.MatchesAllSubTypesWithoutSuffix)
                    {
                        return -1;
                    }
                    else if (!mediaType1.MatchesAllSubTypesWithoutSuffix && mediaType2.MatchesAllSubTypesWithoutSuffix)
                    {
                        return 1;
                    }
                }
                else if (!mediaType1.Suffix.Equals(mediaType2.Suffix, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType1.MatchesAllSubTypesWithoutSuffix)
                    {
                        return -1;
                    }
                    else if (mediaType2.MatchesAllSubTypesWithoutSuffix)
                    {
                        return 1;
                    }
                }
            }

            return returnValue;
        }

        private static int CompareBasedOnQualityFactor(
            MediaTypeHeaderValue mediaType1,
            MediaTypeHeaderValue mediaType2)
        {
            var mediaType1Quality = mediaType1.Quality ?? HeaderQuality.Match;
            var mediaType2Quality = mediaType2.Quality ?? HeaderQuality.Match;
            var qualityDifference = mediaType1Quality - mediaType2Quality;
            if (qualityDifference < 0)
            {
                return -1;
            }
            else if (qualityDifference > 0)
            {
                return 1;
            }

            return 0;
        }
    }
}
