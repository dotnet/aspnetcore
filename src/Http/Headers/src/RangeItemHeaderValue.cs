// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public class RangeItemHeaderValue
    {
        private long? _from;
        private long? _to;

        public RangeItemHeaderValue(long? from, long? to)
        {
            if (!from.HasValue && !to.HasValue)
            {
                throw new ArgumentException("Invalid header range.");
            }
            if (from.HasValue && (from.GetValueOrDefault() < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }
            if (to.HasValue && (to.GetValueOrDefault() < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(to));
            }
            if (from.HasValue && to.HasValue && (from.GetValueOrDefault() > to.GetValueOrDefault()))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }

            _from = from;
            _to = to;
        }

        public long? From
        {
            get { return _from; }
        }

        public long? To
        {
            get { return _to; }
        }

        public override string ToString()
        {
            if (!_from.HasValue)
            {
                return "-" + _to.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (!_to.HasValue)
            {
                return _from.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo) + "-";
            }
            return _from.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo) + "-" +
                _to.GetValueOrDefault().ToString(NumberFormatInfo.InvariantInfo);
        }

        public override bool Equals(object obj)
        {
            if (obj is RangeItemHeaderValue other)
            {
                return ((_from == other._from) && (_to == other._to));
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (!_from.HasValue)
            {
                return _to.GetValueOrDefault().GetHashCode();
            }
            else if (!_to.HasValue)
            {
                return _from.GetValueOrDefault().GetHashCode();
            }
            return _from.GetValueOrDefault().GetHashCode() ^ _to.GetValueOrDefault().GetHashCode();
        }

        // Returns the length of a range list. E.g. "1-2, 3-4, 5-6" adds 3 ranges to 'rangeCollection'. Note that empty
        // list segments are allowed, e.g. ",1-2, , 3-4,,".
        internal static int GetRangeItemListLength(
            StringSegment input,
            int startIndex,
            ICollection<RangeItemHeaderValue> rangeCollection)
        {
            Contract.Requires(rangeCollection != null);
            Contract.Requires(startIndex >= 0);
            Contract.Ensures((Contract.Result<int>() == 0) || (rangeCollection.Count > 0),
                "If we can parse the string, then we expect to have at least one range item.");

            if ((StringSegment.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Empty segments are allowed, so skip all delimiter-only segments (e.g. ", ,").
            var separatorFound = false;
            var current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex, true, out separatorFound);
            // It's OK if we didn't find leading separator characters. Ignore 'separatorFound'.

            if (current == input.Length)
            {
                return 0;
            }

            RangeItemHeaderValue range = null;
            while (true)
            {
                var rangeLength = GetRangeItemLength(input, current, out range);

                if (rangeLength == 0)
                {
                    return 0;
                }

                rangeCollection.Add(range);

                current = current + rangeLength;
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, current, true, out separatorFound);

                // If the string is not consumed, we must have a delimiter, otherwise the string is not a valid
                // range list.
                if ((current < input.Length) && !separatorFound)
                {
                    return 0;
                }

                if (current == input.Length)
                {
                    return current - startIndex;
                }
            }
        }

        internal static int GetRangeItemLength(StringSegment input, int startIndex, out RangeItemHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            // This parser parses number ranges: e.g. '1-2', '1-', '-2'.

            parsedValue = null;

            if (StringSegment.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespaces. If not, we'll return 0.
            var current = startIndex;

            // Try parse the first value of a value pair.
            var fromStartIndex = current;
            var fromLength = HttpRuleParser.GetNumberLength(input, current, false);

            if (fromLength > HttpRuleParser.MaxInt64Digits)
            {
                return 0;
            }

            current = current + fromLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // After the first value, the '-' character must follow.
            if ((current == input.Length) || (input[current] != '-'))
            {
                // We need a '-' character otherwise this can't be a valid range.
                return 0;
            }

            current++; // skip the '-' character
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            var toStartIndex = current;
            var toLength = 0;

            // If we didn't reach the end of the string, try parse the second value of the range.
            if (current < input.Length)
            {
                toLength = HttpRuleParser.GetNumberLength(input, current, false);

                if (toLength > HttpRuleParser.MaxInt64Digits)
                {
                    return 0;
                }

                current = current + toLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            if ((fromLength == 0) && (toLength == 0))
            {
                return 0; // At least one value must be provided in order to be a valid range.
            }

            // Try convert first value to int64
            long from = 0;
            if ((fromLength > 0) && !HeaderUtilities.TryParseNonNegativeInt64(input.Subsegment(fromStartIndex, fromLength), out from))
            {
                return 0;
            }

            // Try convert second value to int64
            long to = 0;
            if ((toLength > 0) && !HeaderUtilities.TryParseNonNegativeInt64(input.Subsegment(toStartIndex, toLength), out to))
            {
                return 0;
            }

            // 'from' must not be greater than 'to'
            if ((fromLength > 0) && (toLength > 0) && (from > to))
            {
                return 0;
            }

            parsedValue = new RangeItemHeaderValue((fromLength == 0 ? (long?)null : (long?)from),
                (toLength == 0 ? (long?)null : (long?)to));
            return current - startIndex;
        }
    }
}
