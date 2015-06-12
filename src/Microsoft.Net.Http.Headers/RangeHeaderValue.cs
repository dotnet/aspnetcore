// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Microsoft.Net.Http.Headers
{
    public class RangeHeaderValue
    {
        private static readonly HttpHeaderParser<RangeHeaderValue> Parser
            = new GenericHeaderParser<RangeHeaderValue>(false, GetRangeLength);

        private string _unit;
        private ICollection<RangeItemHeaderValue> _ranges;

        public RangeHeaderValue()
        {
            _unit = HeaderUtilities.BytesUnit;
        }

        public RangeHeaderValue(long? from, long? to)
        {
            // convenience ctor: "Range: bytes=from-to"
            _unit = HeaderUtilities.BytesUnit;
            Ranges.Add(new RangeItemHeaderValue(from, to));
        }

        public string Unit
        {
            get { return _unit; }
            set
            {
                HeaderUtilities.CheckValidToken(value, nameof(value));
                _unit = value;
            }
        }

        public ICollection<RangeItemHeaderValue> Ranges
        {
            get
            {
                if (_ranges == null)
                {
                    _ranges = new ObjectCollection<RangeItemHeaderValue>();
                }
                return _ranges;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(_unit);
            sb.Append('=');

            var first = true;
            foreach (var range in Ranges)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(range.From);
                sb.Append('-');
                sb.Append(range.To);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as RangeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return (string.Compare(_unit, other._unit, StringComparison.OrdinalIgnoreCase) == 0) &&
                HeaderUtilities.AreEqualCollections(Ranges, other.Ranges);
        }

        public override int GetHashCode()
        {
            var result = StringComparer.OrdinalIgnoreCase.GetHashCode(_unit);

            foreach (var range in Ranges)
            {
                result = result ^ range.GetHashCode();
            }

            return result;
        }

        public static RangeHeaderValue Parse(string input)
        {
            var index = 0;
            return Parser.ParseValue(input, ref index);
        }

        public static bool TryParse(string input, out RangeHeaderValue parsedValue)
        {
            var index = 0;
            return Parser.TryParseValue(input, ref index, out parsedValue);
        }

        private static int GetRangeLength(string input, int startIndex, out RangeHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the unit string: <unit> in '<unit>=<from1>-<to1>, <from2>-<to2>'
            var unitLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (unitLength == 0)
            {
                return 0;
            }

            RangeHeaderValue result = new RangeHeaderValue();
            result._unit = input.Substring(startIndex, unitLength);
            var current = startIndex + unitLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != '='))
            {
                return 0;
            }

            current++; // skip '=' separator
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            var rangesLength = RangeItemHeaderValue.GetRangeItemListLength(input, current, result.Ranges);

            if (rangesLength == 0)
            {
                return 0;
            }

            current = current + rangesLength;
            Contract.Assert(current == input.Length, "GetRangeItemListLength() should consume the whole string or fail.");

            parsedValue = result;
            return current - startIndex;
        }
    }
}
