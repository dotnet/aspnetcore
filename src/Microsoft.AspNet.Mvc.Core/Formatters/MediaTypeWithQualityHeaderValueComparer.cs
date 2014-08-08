// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Implementation of <see cref="IComparer{T}"/> that can compare accept media type header fields
    /// based on their quality values (a.k.a q-values).
    /// </summary>
    public class MediaTypeWithQualityHeaderValueComparer : IComparer<MediaTypeWithQualityHeaderValue>
    {
        private static readonly MediaTypeWithQualityHeaderValueComparer _mediaTypeComparer = 
                                                                    new MediaTypeWithQualityHeaderValueComparer();

        private MediaTypeWithQualityHeaderValueComparer()
        {
        }

        public static MediaTypeWithQualityHeaderValueComparer QualityComparer
        {
            get { return _mediaTypeComparer; }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Performs comparisons based on the arguments' quality values
        /// (aka their "q-value"). Values with identical q-values are considered equal (i.e. the result is 0)
        /// with the exception that subtype wildcards are considered less than specific media types and full
        /// wildcards are considered less than subtype wildcards. This allows callers to sort a sequence of
        /// <see cref="MediaTypeWithQualityHeaderValue"/> following their q-values in the order of specific
        /// media types, subtype wildcards, and last any full wildcards.
        /// </remarks>
        public int Compare(MediaTypeWithQualityHeaderValue mediaType1, MediaTypeWithQualityHeaderValue mediaType2)
        {
            if (object.ReferenceEquals(mediaType1, mediaType2))
            {
                return 0;
            }

            var returnValue = CompareBasedOnQualityFactor(mediaType1, mediaType2);

            if (returnValue == 0)
            {
                if (!mediaType1.MediaType.Equals(mediaType2.MediaType, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType1.MediaTypeRange == MediaTypeHeaderValueRange.AllMediaRange)
                    {
                        return -1;
                    }
                    else if (mediaType2.MediaTypeRange == MediaTypeHeaderValueRange.AllMediaRange)
                    {
                        return 1;
                    }
                    else if (mediaType1.MediaTypeRange == MediaTypeHeaderValueRange.SubtypeMediaRange &&
                             mediaType2.MediaTypeRange != MediaTypeHeaderValueRange.SubtypeMediaRange)
                    {
                        return -1;
                    }
                    else if (mediaType1.MediaTypeRange != MediaTypeHeaderValueRange.SubtypeMediaRange &&
                             mediaType2.MediaTypeRange == MediaTypeHeaderValueRange.SubtypeMediaRange)
                    {
                        return 1;
                    }
                }
                else if (!mediaType1.MediaSubType.Equals(mediaType2.MediaSubType, StringComparison.OrdinalIgnoreCase))
                {
                    if (mediaType1.MediaTypeRange == MediaTypeHeaderValueRange.SubtypeMediaRange)
                    {
                        return -1;
                    }
                    else if (mediaType2.MediaTypeRange == MediaTypeHeaderValueRange.SubtypeMediaRange)
                    {
                        return 1;
                    }
                }
            }

            return returnValue;
        }

        private static int CompareBasedOnQualityFactor(MediaTypeWithQualityHeaderValue mediaType1,
                                                       MediaTypeWithQualityHeaderValue mediaType2)
        {
            var mediaType1Quality = mediaType1.Quality ?? HttpHeaderUtilitites.Match;
            var mediaType2Quality = mediaType2.Quality ?? HttpHeaderUtilitites.Match;
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
