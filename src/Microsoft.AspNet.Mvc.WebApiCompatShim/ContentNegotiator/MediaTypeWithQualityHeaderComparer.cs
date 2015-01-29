// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace System.Net.Http.Formatting
{
    /// Implementation of <see cref="IComparer{T}"/> that can compare accept media type header fields
    /// based on their quality values (a.k.a q-values). See
    /// <see cref="StringWithQualityHeaderValueComparer"/> for a comparer for other content negotiation
    /// header field q-values.
    internal class MediaTypeWithQualityHeaderValueComparer : IComparer<MediaTypeWithQualityHeaderValue>
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

        /// <summary>
        /// Compares two <see cref="MediaTypeWithQualityHeaderValue"/> based on their quality value (a.k.a their
        /// "q-value"). Values with identical q-values are considered equal (i.e the result is 0) with the exception
        /// that sub-type wild-cards are considered less than specific media types and full wild-cards are considered
        /// less than sub-type wild-cards. This allows to sort a sequence of <see cref="StringWithQualityHeaderValue"/>
        /// following their q-values in the order of specific media types, subtype wild-cards, and last any full
        /// wild-cards.
        /// </summary>
        /// <param name="mediaType1">The first <see cref="MediaTypeWithQualityHeaderValue"/> to compare.</param>
        /// <param name="mediaType2">The second <see cref="MediaTypeWithQualityHeaderValue"/> to compare.</param>
        /// <returns></returns>
        public int Compare(MediaTypeWithQualityHeaderValue mediaType1, MediaTypeWithQualityHeaderValue mediaType2)
        {
            Debug.Assert(mediaType1 != null, "The 'mediaType1' parameter should not be null.");
            Debug.Assert(mediaType2 != null, "The 'mediaType2' parameter should not be null.");

            if (Object.ReferenceEquals(mediaType1, mediaType2))
            {
                return 0;
            }

            var returnValue = CompareBasedOnQualityFactor(mediaType1, mediaType2);
            if (returnValue == 0)
            {
                var parsedMediaType1 = new ParsedMediaTypeHeaderValue(mediaType1);
                var parsedMediaType2 = new ParsedMediaTypeHeaderValue(mediaType2);

                if (!parsedMediaType1.TypesEqual(ref parsedMediaType2))
                {
                    if (parsedMediaType1.IsAllMediaRange)
                    {
                        return -1;
                    }
                    else if (parsedMediaType2.IsAllMediaRange)
                    {
                        return 1;
                    }
                    else if (parsedMediaType1.IsSubtypeMediaRange && !parsedMediaType2.IsSubtypeMediaRange)
                    {
                        return -1;
                    }
                    else if (!parsedMediaType1.IsSubtypeMediaRange && parsedMediaType2.IsSubtypeMediaRange)
                    {
                        return 1;
                    }
                }
                else if (!parsedMediaType1.SubTypesEqual(ref parsedMediaType2))
                {
                    if (parsedMediaType1.IsSubtypeMediaRange)
                    {
                        return -1;
                    }
                    else if (parsedMediaType2.IsSubtypeMediaRange)
                    {
                        return 1;
                    }
                }
            }

            return returnValue;
        }

        private static int CompareBasedOnQualityFactor(
            MediaTypeWithQualityHeaderValue mediaType1,
            MediaTypeWithQualityHeaderValue mediaType2)
        {
            Debug.Assert(mediaType1 != null);
            Debug.Assert(mediaType2 != null);

            var mediaType1Quality = mediaType1.Quality ?? FormattingUtilities.Match;
            var mediaType2Quality = mediaType2.Quality ?? FormattingUtilities.Match;
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
#endif