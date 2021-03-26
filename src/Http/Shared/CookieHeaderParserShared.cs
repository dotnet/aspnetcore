// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    internal static class CookieHeaderParserShared
    {
        public static bool TryParseValues(StringValues values, Dictionary<string, string> store, bool enableCookieNameEncoding, bool supportsMultipleValues)
        {
            // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            if (values.Count == 0)
            {
                return false;
            }
            bool hasFoundValue = false;

            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                var index = 0;

                while (!string.IsNullOrEmpty(value) && index < value.Length)
                {
                    if (TryParseValue(value, ref index, supportsMultipleValues, out var parsedName, out var parsedValue))
                    {
                        // The entry may not contain an actual value, like " , "
                        if (parsedName != null && parsedValue != null)
                        {
                            var name = enableCookieNameEncoding ? Uri.UnescapeDataString(parsedName.Value.Value) : parsedName.Value.Value;
                            store[name] = Uri.UnescapeDataString(parsedValue.Value.Value);
                            hasFoundValue = true;
                        }
                    }
                    else
                    {
                        // Skip the invalid values and keep trying.
                        index++;
                    }
                }
            }

            return hasFoundValue;
        }

        public static bool TryParseValue(StringSegment value, ref int index, bool supportsMultipleValues, [NotNullWhen(true)] out StringSegment? parsedName, [NotNullWhen(true)] out StringSegment? parsedValue)
        {
            parsedName = null;
            parsedValue = null;

            // If multiple values are supported (i.e. list of values), then accept an empty string: The header may
            // be added multiple times to the request/response message. E.g.
            //  Accept: text/xml; q=1
            //  Accept:
            //  Accept: text/plain; q=0.2
            if (StringSegment.IsNullOrEmpty(value) || (index == value.Length))
            {
                return supportsMultipleValues;
            }

            var current = GetNextNonEmptyOrWhitespaceIndex(value, index, supportsMultipleValues, out bool separatorFound);

            if (separatorFound && !supportsMultipleValues)
            {
                return false; // leading separators not allowed if we don't support multiple values.
            }

            if (current == value.Length)
            {
                if (supportsMultipleValues)
                {
                    index = current;
                }
                return supportsMultipleValues;
            }

            if (!CookieHeaderValue.TryGetCookieLength(value, ref current, out parsedName, out parsedValue))
            {
                return false;
            }

            current = GetNextNonEmptyOrWhitespaceIndex(value, current, supportsMultipleValues, out separatorFound);

            // If we support multiple values and we've not reached the end of the string, then we must have a separator.
            if ((separatorFound && !supportsMultipleValues) || (!separatorFound && (current < value.Length)))
            {
                return false;
            }

            index = current;

            return true;
        }

        private static int GetNextNonEmptyOrWhitespaceIndex(StringSegment input, int startIndex, bool skipEmptyValues, out bool separatorFound)
        {
            Contract.Requires(startIndex <= input.Length); // it's OK if index == value.Length.

            separatorFound = false;
            var current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);

            if ((current == input.Length) || (input[current] != ',' && input[current] != ';'))
            {
                return current;
            }

            // If we have a separator, skip the separator and all following whitespaces. If we support
            // empty values, continue until the current character is neither a separator nor a whitespace.
            separatorFound = true;
            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if (skipEmptyValues)
            {
                // Most headers only split on ',', but cookies primarily split on ';'
                while ((current < input.Length) && ((input[current] == ',') || (input[current] == ';')))
                {
                    current++; // skip delimiter.
                    current = current + HttpRuleParser.GetWhitespaceLength(input, current);
                }
            }

            return current;
        }
    }
}
