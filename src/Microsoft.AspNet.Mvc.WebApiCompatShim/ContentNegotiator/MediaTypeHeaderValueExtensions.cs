// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNETCORE50

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNet.Mvc;

namespace System.Net.Http.Formatting
{
    /// <summary>
    /// Extension methods for <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    internal static class MediaTypeHeaderValueExtensions
    {
        /// <summary>
        /// Determines whether two <see cref="MediaTypeHeaderValue"/> instances match. The instance
        /// <paramref name="mediaType1"/> is said to match <paramref name="mediaType2"/> if and only if
        /// <paramref name="mediaType1"/> is a strict subset of the values and parameters of
        /// <paramref name="mediaType2"/>.
        /// That is, if the media type and media type parameters of <paramref name="mediaType1"/> are all present
        /// and match those of <paramref name="mediaType2"/> then it is a match even though
        /// <paramref name="mediaType2"/> may have additional parameters.
        /// </summary>
        /// <param name="mediaType1">The first media type.</param>
        /// <param name="mediaType2">The second media type.</param>
        /// <returns><c>true</c> if this is a subset of <paramref name="mediaType2"/>; false otherwise.</returns>
        public static bool IsSubsetOf(this MediaTypeHeaderValue mediaType1, MediaTypeHeaderValue mediaType2)
        {
            MediaTypeHeaderValueRange mediaType2Range;
            return IsSubsetOf(mediaType1, mediaType2, out mediaType2Range);
        }

        /// <summary>
        /// Determines whether two <see cref="MediaTypeHeaderValue"/> instances match. The instance
        /// <paramref name="mediaType1"/> is said to match <paramref name="mediaType2"/> if and only if
        /// <paramref name="mediaType1"/> is a strict subset of the values and parameters of
        /// <paramref name="mediaType2"/>.
        /// That is, if the media type and media type parameters of <paramref name="mediaType1"/> are all present
        /// and match those of <paramref name="mediaType2"/> then it is a match even though
        /// <paramref name="mediaType2"/> may have additional parameters.
        /// </summary>
        /// <param name="mediaType1">The first media type.</param>
        /// <param name="mediaType2">The second media type.</param>
        /// <param name="mediaType2Range">
        /// Indicates whether <paramref name="mediaType2"/> is a regular media type, a subtype media range, or a full
        /// media range.
        /// </param>
        /// <returns><c>true</c> if this is a subset of <paramref name="mediaType2"/>; false otherwise.</returns>
        public static bool IsSubsetOf(
            this MediaTypeHeaderValue mediaType1,
            MediaTypeHeaderValue mediaType2,
            out MediaTypeHeaderValueRange mediaType2Range)
        {
            // Performance-sensitive
            Debug.Assert(mediaType1 != null);

            if (mediaType2 == null)
            {
                mediaType2Range = MediaTypeHeaderValueRange.None;
                return false;
            }

            var parsedMediaType1 = new ParsedMediaTypeHeaderValue(mediaType1);
            var parsedMediaType2 = new ParsedMediaTypeHeaderValue(mediaType2);
            mediaType2Range = parsedMediaType2.IsAllMediaRange ? MediaTypeHeaderValueRange.AllMediaRange :
                parsedMediaType2.IsSubtypeMediaRange ? MediaTypeHeaderValueRange.SubtypeMediaRange :
                MediaTypeHeaderValueRange.None;

            if (!parsedMediaType1.TypesEqual(ref parsedMediaType2))
            {
                if (mediaType2Range != MediaTypeHeaderValueRange.AllMediaRange)
                {
                    return false;
                }
            }
            else if (!parsedMediaType1.SubTypesEqual(ref parsedMediaType2))
            {
                if (mediaType2Range != MediaTypeHeaderValueRange.SubtypeMediaRange)
                {
                    return false;
                }
            }

            // So far we either have a full match or a subset match. Now check that all of
            // mediaType1's parameters are present and equal in mediatype2
            // Optimize for the common case where the parameters inherit from Collection<T> and cache the count which
            // is faster for Collection<T>.
            var parameters1 = mediaType1.Parameters.AsCollection();
            var parameterCount1 = parameters1.Count;
            var parameters2 = mediaType2.Parameters.AsCollection();
            var parameterCount2 = parameters2.Count;
            for (var i = 0; i < parameterCount1; i++)
            {
                var parameter1 = parameters1[i];
                var found = false;
                for (var j = 0; j < parameterCount2; j++)
                {
                    var parameter2 = parameters2[j];
                    if (parameter1.Equals(parameter2))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif