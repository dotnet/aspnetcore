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
        public static bool TryParseValues(StringValues values, IDictionary<string, string> store, bool enableCookieNameEncoding, bool supportsMultipleValues)
        {
            // If a parser returns an empty list, it means there was no value, but that's valid (e.g. "Accept: "). The caller
            // can ignore the value.
            if (values.Count == 0)
            {
                return false;
            }
            var hasFoundValue = false;

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

            var current = GetNextNonEmptyOrWhitespaceIndex(value, index, supportsMultipleValues, out var separatorFound);

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

            if (!TryGetCookieLength(value, ref current, out parsedName, out parsedValue))
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

        // name=value; name="value"
        internal static bool TryGetCookieLength(StringSegment input, ref int offset, [NotNullWhen(true)] out StringSegment? parsedName, [NotNullWhen(true)] out StringSegment? parsedValue)
        {
            Contract.Requires(offset >= 0);

            parsedName = null;
            parsedValue = null;

            if (StringSegment.IsNullOrEmpty(input) || (offset >= input.Length))
            {
                return false;
            }

            // The caller should have already consumed any leading whitespace, commas, etc..

            // Name=value;

            // Name
            var itemLength = HttpRuleParser.GetTokenLength(input, offset);
            if (itemLength == 0)
            {
                return false;
            }

            parsedName = input.Subsegment(offset, itemLength);
            offset += itemLength;

            // = (no spaces)
            if (!ReadEqualsSign(input, ref offset))
            {
                return false;
            }

            // value or "quoted value"
            // The value may be empty
            parsedValue = GetCookieValue(input, ref offset);

            return true;
        }

        // cookie-value      = *cookie-octet / ( DQUOTE* cookie-octet DQUOTE )
        // cookie-octet      = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                     ; US-ASCII characters excluding CTLs, whitespace DQUOTE, comma, semicolon, and backslash
        internal static StringSegment GetCookieValue(StringSegment input, ref int offset)
        {
            Contract.Requires(offset >= 0);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - offset)));

            var startIndex = offset;

            if (offset >= input.Length)
            {
                return StringSegment.Empty;
            }
            var inQuotes = false;

            if (input[offset] == '"')
            {
                inQuotes = true;
                offset++;
            }

            while (offset < input.Length)
            {
                var c = input[offset];
                if (!IsCookieValueChar(c))
                {
                    break;
                }

                offset++;
            }

            if (inQuotes)
            {
                if (offset == input.Length || input[offset] != '"')
                {
                    // Missing final quote
                    return StringSegment.Empty;
                }
                offset++;
            }

            var length = offset - startIndex;
            if (offset > startIndex)
            {
                return input.Subsegment(startIndex, length);
            }

            return StringSegment.Empty;
        }

        private static bool ReadEqualsSign(StringSegment input, ref int offset)
        {
            // = (no spaces)
            if (offset >= input.Length || input[offset] != '=')
            {
                return false;
            }
            offset++;
            return true;
        }

        // cookie-octet      = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                     ; US-ASCII characters excluding CTLs, whitespace DQUOTE, comma, semicolon, and backslash
        private static bool IsCookieValueChar(char c)
        {
            if (c < 0x21 || c > 0x7E)
            {
                return false;
            }
            return !(c == '"' || c == ',' || c == ';' || c == '\\');
        }
    }
}
