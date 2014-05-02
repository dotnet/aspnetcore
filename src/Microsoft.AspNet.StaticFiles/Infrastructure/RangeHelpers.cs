// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.StaticFiles.Infrastructure
{
    internal static class RangeHelpers
    {
        // Examples:
        // bytes=0-499
        // bytes=500-
        // bytes=-500
        // bytes=0-0,-1
        // bytes=500-600,601-999
        // Any individual bad range fails the whole parse and the header should be ignored.
        internal static bool TryParseRanges(string rangeHeader, out IList<Tuple<long?, long?>> parsedRanges)
        {
            parsedRanges = null;
            if (string.IsNullOrWhiteSpace(rangeHeader)
                || !rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] subRanges = rangeHeader.Substring("bytes=".Length).Replace(" ", string.Empty).Split(',');

            List<Tuple<long?, long?>> ranges = new List<Tuple<long?, long?>>();

            for (int i = 0; i < subRanges.Length; i++)
            {
                long? first = null, second = null;
                string subRange = subRanges[i];
                int dashIndex = subRange.IndexOf('-');
                if (dashIndex < 0)
                {
                    return false;
                }
                else if (dashIndex == 0)
                {
                    // -500
                    string remainder = subRange.Substring(1);
                    if (!TryParseLong(remainder, out second))
                    {
                        return false;
                    }
                }
                else if (dashIndex == (subRange.Length - 1))
                {
                    // 500-
                    string remainder = subRange.Substring(0, subRange.Length - 1);
                    if (!TryParseLong(remainder, out first))
                    {
                        return false;
                    }
                }
                else
                {
                    // 0-499
                    string firstString = subRange.Substring(0, dashIndex);
                    string secondString = subRange.Substring(dashIndex + 1, subRange.Length - dashIndex - 1);
                    if (!TryParseLong(firstString, out first) || !TryParseLong(secondString, out second)
                        || first.Value > second.Value)
                    {
                        return false;
                    }
                }

                ranges.Add(new Tuple<long?, long?>(first, second));
            }

            if (ranges.Count > 0)
            {
                parsedRanges = ranges;
                return true;
            }
            return false;
        }

        private static bool TryParseLong(string input, out long? result)
        {
            int temp;
            if (!string.IsNullOrWhiteSpace(input)
                && int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out temp))
            {
                result = temp;
                return true;
            }
            result = null;
            return false;
        }

        // 14.35.1 Byte Ranges - If a syntactically valid byte-range-set includes at least one byte-range-spec whose
        // first-byte-pos is less than the current length of the entity-body, or at least one suffix-byte-range-spec
        // with a non-zero suffix-length, then the byte-range-set is satisfiable.
        // Adjusts ranges to be absolute and within bounds.
        internal static IList<Tuple<long, long>> NormalizeRanges(IList<Tuple<long?, long?>> ranges, long length)
        {
            IList<Tuple<long, long>> normalizedRanges = new List<Tuple<long, long>>(ranges.Count);
            for (int i = 0; i < ranges.Count; i++)
            {
                Tuple<long?, long?> range = ranges[i];
                long? start = range.Item1, end = range.Item2;

                // X-[Y]
                if (start.HasValue)
                {
                    if (start.Value >= length)
                    {
                        // Not satisfiable, skip/discard.
                        continue;
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
                        continue;
                    }

                    long bytes = Math.Min(end.Value, length);
                    start = length - bytes;
                    end = start + bytes - 1;
                }
                normalizedRanges.Add(new Tuple<long, long>(start.Value, end.Value));
            }
            return normalizedRanges;
        }
    }
}
