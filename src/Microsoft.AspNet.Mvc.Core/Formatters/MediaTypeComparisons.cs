// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Different types of tests against media type values.
    /// </summary>
    public static class MediaTypeComparisons
    {
        /// <summary>
        /// Determines if the <paramref name="subset" /> media type is a subset of the <paramref name="set" /> media type
        /// without taking into account the quality parameter.
        /// </summary>
        /// <param name="set">The more general media type.</param>
        /// <param name="subset">The more specific media type.</param>
        /// <returns><code>true</code> if <paramref name="set" /> is a more general media type than <paramref name="subset"/>;
        /// otherwise <code>false</code>.</returns>
        public static bool IsSubsetOf(StringSegment set, string subset)
        {
            return IsSubsetOf(set, new StringSegment(subset));
        }

        /// <summary>
        /// Determines if the <paramref name="subset" /> media type is a subset of the <paramref name="set" /> media type
        /// without taking into account the quality parameter.
        /// </summary>
        /// <param name="set">The more general media type.</param>
        /// <param name="subset">The more specific media type.</param>
        /// <returns><code>true</code> if <paramref name="set" /> is a more general media type than <paramref name="subset"/>;
        /// otherwise <code>false</code>.</returns>
        public static bool IsSubsetOf(string set, string subset)
        {
            return IsSubsetOf(new StringSegment(set), new StringSegment(subset));
        }

        /// <summary>
        /// Determines if the <paramref name="subset" /> media type is a subset of the <paramref name="set" /> media type.
        /// Two media types are compatible if one is a subset of the other ignoring any charset
        /// parameter.
        /// </summary>
        /// <param name="set">The more general media type.</param>
        /// <param name="subset">The more specific media type.</param>
        /// <param name="ignoreQuality">Whether or not we should skip checking the quality parameter.</param>
        /// <returns><code>true</code> if <paramref name="set" /> is a more general media type than <paramref name="subset"/>;
        /// otherwise <code>false</code>.</returns>
        public static bool IsSubsetOf(StringSegment set, StringSegment subset)
        {
            if (!set.HasValue || !subset.HasValue)
            {
                return false;
            }

            MediaTypeHeaderValue setMediaType;
            MediaTypeHeaderValue subSetMediaType;

            return MediaTypeHeaderValue.TryParse(set.Value, out setMediaType) &&
                MediaTypeHeaderValue.TryParse(subset.Value, out subSetMediaType) &&
                subSetMediaType.IsSubsetOf(setMediaType);
        }

        /// <summary>
        /// Determines if the type of a given <paramref name="mediaType" /> matches all types, E.g, */*.
        /// </summary>
        /// <param name="mediaType">The media type to check</param>
        /// <returns><code>true</code> if the <paramref name="mediaType" /> matches all subtypes; otherwise <code>false</code>.</returns>
        public static bool MatchesAllTypes(string mediaType)
        {
            return MatchesAllTypes(new StringSegment(mediaType));
        }

        /// <summary>
        /// Determines if the type of a given <paramref name="mediaType" /> matches all types, E.g, */*.
        /// </summary>
        /// <param name="mediaType">The media type to check</param>
        /// <returns><code>true</code> if the <paramref name="mediaType" /> matches all subtypes; otherwise <code>false</code>.</returns>
        public static bool MatchesAllTypes(StringSegment mediaType)
        {
            if (!mediaType.HasValue)
            {
                return false;
            }

            MediaTypeHeaderValue parsedMediaType;
            return MediaTypeHeaderValue.TryParse(mediaType.Value, out parsedMediaType) &&
                parsedMediaType.MatchesAllTypes;
        }

        /// <summary>
        /// Determines if the given <paramref name="mediaType" /> matches all subtypes, E.g, text/*.
        /// </summary>
        /// <param name="mediaType">The media type to check</param>
        /// <returns><code>true</code> if the <paramref name="mediaType" /> matches all subtypes; otherwise <code>false</code>.</returns>
        public static bool MatchesAllSubtypes(string mediaType)
        {
            return MatchesAllSubtypes(new StringSegment(mediaType));
        }

        /// <summary>
        /// Determines if the given <paramref name="mediaType" /> matches all subtypes, E.g, text/*.
        /// </summary>
        /// <param name="mediaType">The media type to check</param>
        /// <returns><code>true</code> if the <paramref name="mediaType" /> matches all subtypes; otherwise <code>false</code>.</returns>
        public static bool MatchesAllSubtypes(StringSegment mediaType)
        {
            if (!mediaType.HasValue)
            {
                return false;
            }

            MediaTypeHeaderValue parsedMediaType;
            return MediaTypeHeaderValue.TryParse(mediaType.Value, out parsedMediaType) &&
                parsedMediaType.MatchesAllSubTypes;
        }
    }
}
