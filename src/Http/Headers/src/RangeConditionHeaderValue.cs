// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    public class RangeConditionHeaderValue
    {
        private static readonly HttpHeaderParser<RangeConditionHeaderValue> Parser
            = new GenericHeaderParser<RangeConditionHeaderValue>(false, GetRangeConditionLength);

        private DateTimeOffset? _lastModified;
        private EntityTagHeaderValue _entityTag;

        private RangeConditionHeaderValue()
        {
            // Used by the parser to create a new instance of this type.
        }

        public RangeConditionHeaderValue(DateTimeOffset lastModified)
        {
            _lastModified = lastModified;
        }

        public RangeConditionHeaderValue(EntityTagHeaderValue entityTag)
        {
            if (entityTag == null)
            {
                throw new ArgumentNullException(nameof(entityTag));
            }

            _entityTag = entityTag;
        }

        public RangeConditionHeaderValue(string entityTag)
            : this(new EntityTagHeaderValue(entityTag))
        {
        }

        public DateTimeOffset? LastModified
        {
            get { return _lastModified; }
        }

        public EntityTagHeaderValue EntityTag
        {
            get { return _entityTag; }
        }

        public override string ToString()
        {
            if (_entityTag == null)
            {
                return HeaderUtilities.FormatDate(_lastModified.GetValueOrDefault());
            }
            return _entityTag.ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as RangeConditionHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (_entityTag == null)
            {
                return (other._lastModified != null) && (_lastModified.GetValueOrDefault() == other._lastModified.GetValueOrDefault());
            }

            return _entityTag.Equals(other._entityTag);
        }

        public override int GetHashCode()
        {
            if (_entityTag == null)
            {
                return _lastModified.GetValueOrDefault().GetHashCode();
            }

            return _entityTag.GetHashCode();
        }

        public static RangeConditionHeaderValue Parse(StringSegment input)
        {
            var index = 0;
            return Parser.ParseValue(input, ref index);
        }

        public static bool TryParse(StringSegment input, out RangeConditionHeaderValue parsedValue)
        {
            var index = 0;
            return Parser.TryParseValue(input, ref index, out parsedValue);
        }

        private static int GetRangeConditionLength(StringSegment input, int startIndex, out RangeConditionHeaderValue parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            // Make sure we have at least 2 characters
            if (StringSegment.IsNullOrEmpty(input) || (startIndex + 1 >= input.Length))
            {
                return 0;
            }

            var current = startIndex;

            // Caller must remove leading whitespaces.
            DateTimeOffset date = DateTimeOffset.MinValue;
            EntityTagHeaderValue entityTag = null;

            // Entity tags are quoted strings optionally preceded by "W/". By looking at the first two character we
            // can determine whether the string is en entity tag or a date.
            var firstChar = input[current];
            var secondChar = input[current + 1];

            if ((firstChar == '\"') || (((firstChar == 'w') || (firstChar == 'W')) && (secondChar == '/')))
            {
                // trailing whitespaces are removed by GetEntityTagLength()
                var entityTagLength = EntityTagHeaderValue.GetEntityTagLength(input, current, out entityTag);

                if (entityTagLength == 0)
                {
                    return 0;
                }

                current = current + entityTagLength;

                // RangeConditionHeaderValue only allows 1 value. There must be no delimiter/other chars after an
                // entity tag.
                if (current != input.Length)
                {
                    return 0;
                }
            }
            else
            {
                if (!HttpRuleParser.TryStringToDate(input.Subsegment(current), out date))
                {
                    return 0;
                }

                // If we got a valid date, then the parser consumed the whole string (incl. trailing whitespaces).
                current = input.Length;
            }

            parsedValue = new RangeConditionHeaderValue();
            if (entityTag == null)
            {
                parsedValue._lastModified = date;
            }
            else
            {
                parsedValue._entityTag = entityTag;
            }

            return current - startIndex;
        }
    }
}