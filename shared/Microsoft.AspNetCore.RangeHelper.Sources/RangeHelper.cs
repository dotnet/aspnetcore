// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Internal
{
    /// <summary>
    /// Provides a parser for the Range Header in an <see cref="HttpContext.Request"/>.
    /// </summary>
    internal static class RangeHelper
    {
        /// <summary>
        /// Returns the requested range if the Range Header in the <see cref="HttpContext.Request"/> is valid.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the request.</param>
        /// <param name="requestHeaders">The <see cref="RequestHeaders"/> associated with the given <paramref name="context"/>.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> representation of the last modified date of the file.</param>
        /// <param name="etag">The <see cref="EntityTagHeaderValue"/> provided in the <see cref="HttpContext.Request"/>.</param>
        /// <returns>A collection of <see cref="RangeItemHeaderValue"/> containing the ranges parsed from the <paramref name="requestHeaders"/>.</returns>
        public static ICollection<RangeItemHeaderValue> ParseRange(HttpContext context, RequestHeaders requestHeaders, DateTimeOffset? lastModified = null, EntityTagHeaderValue etag = null)
        {
            var rawRangeHeader = context.Request.Headers[HeaderNames.Range];
            if (StringValues.IsNullOrEmpty(rawRangeHeader))
            {
                return null;
            }

            // Perf: Check for a single entry before parsing it
            if (rawRangeHeader.Count > 1 || rawRangeHeader[0].IndexOf(',') >= 0)
            {
                // The spec allows for multiple ranges but we choose not to support them because the client may request
                // very strange ranges (e.g. each byte separately, overlapping ranges, etc.) that could negatively
                // impact the server. Ignore the header and serve the response normally.               
                return null;
            }

            var rangeHeader = requestHeaders.Range;
            if (rangeHeader == null)
            {
                // Invalid
                return null;
            }

            // Already verified above
            Debug.Assert(rangeHeader.Ranges.Count == 1);

            // 14.27 If-Range
            var ifRangeHeader = requestHeaders.IfRange;
            if (ifRangeHeader != null)
            {
                // If the validator given in the If-Range header field matches the
                // current validator for the selected representation of the target
                // resource, then the server SHOULD process the Range header field as
                // requested.  If the validator does not match, the server MUST ignore
                // the Range header field.
                bool ignoreRangeHeader = false;
                if (ifRangeHeader.LastModified.HasValue)
                {
                    if (lastModified.HasValue && lastModified > ifRangeHeader.LastModified)
                    {
                        ignoreRangeHeader = true;
                    }
                }
                else if (etag != null && ifRangeHeader.EntityTag != null && !ifRangeHeader.EntityTag.Compare(etag, useStrongComparison: true))
                {
                    ignoreRangeHeader = true;
                }

                if (ignoreRangeHeader)
                {
                    return null;
                }
            }
          
            return rangeHeader.Ranges;
        }

        /// <summary>
        /// A helper method to normalize a collection of <see cref="RangeItemHeaderValue"/>s.
        /// </summary>
        /// <param name="ranges">A collection of <see cref="RangeItemHeaderValue"/> to normalize.</param>
        /// <param name="length">The total length of the file representation requested.</param>
        /// <returns>A normalized list of <see cref="RangeItemHeaderValue"/>s.</returns>
        // 14.35.1 Byte Ranges - If a syntactically valid byte-range-set includes at least one byte-range-spec whose
        // first-byte-pos is less than the current length of the entity-body, or at least one suffix-byte-range-spec
        // with a non-zero suffix-length, then the byte-range-set is satisfiable.
        // Adjusts ranges to be absolute and within bounds.
        public static IList<RangeItemHeaderValue> NormalizeRanges(ICollection<RangeItemHeaderValue> ranges, long length)
        {
            if (ranges == null)
            {
                return null;
            }

            if (ranges.Count == 0)
            {
                return Array.Empty<RangeItemHeaderValue>();
            }

            if (length == 0)
            {
                return Array.Empty<RangeItemHeaderValue>();
            }

            var normalizedRanges = new List<RangeItemHeaderValue>(ranges.Count);
            foreach (var range in ranges)
            {
                var normalizedRange = NormalizeRange(range, length);

                if (normalizedRange != null)
                {
                    normalizedRanges.Add(normalizedRange);
                }
            }

            return normalizedRanges;
        }

        /// <summary>
        /// A helper method to normalize a <see cref="RangeItemHeaderValue"/>.
        /// </summary>
        /// <param name="range">The <see cref="RangeItemHeaderValue"/> to normalize.</param>
        /// <param name="length">The total length of the file representation requested.</param>
        /// <returns>A normalized <see cref="RangeItemHeaderValue"/>.</returns>
        public static RangeItemHeaderValue NormalizeRange(RangeItemHeaderValue range, long length)
        {
            var start = range.From;
            var end = range.To;

            // X-[Y]
            if (start.HasValue)
            {
                if (start.Value >= length)
                {
                    // Not satisfiable, skip/discard.
                    return null;
                }
                if (!end.HasValue || end.Value >= length)
                {
                    end = length - 1;
                }
            }
            else
            {
                // suffix range "-X" e.g. the last X bytes, resolve
                if (end.Value == 0)
                {
                    // Not satisfiable, skip/discard.
                    return null;
                }

                var bytes = Math.Min(end.Value, length);
                start = length - bytes;
                end = start + bytes - 1;
            }

            var normalizedRange = new RangeItemHeaderValue(start, end);
            return normalizedRange;
        }
    }
}
