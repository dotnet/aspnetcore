// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;

namespace Microsoft.Net.Http.Headers
{
    internal class CookieHeaderParser : HttpHeaderParser<CookieHeaderValue>
    {
        // The Cache-Control header is special: It is a header supporting a list of values, but we represent the list
        // as _one_ instance of CacheControlHeaderValue. I.e we set 'SupportsMultipleValues' to 'true' since it is
        // OK to have multiple Cache-Control headers in a request/response message. However, after parsing all
        // Cache-Control headers, only one instance of CacheControlHeaderValue is created (if all headers contain valid
        // values, otherwise we may have multiple strings containing the invalid values).
        internal CookieHeaderParser(bool supportsMultipleValues)
            : base(supportsMultipleValues)
        {
        }

        public sealed override bool TryParseValue(string value, ref int index, out CookieHeaderValue parsedValue)
        {
            parsedValue = null;

            // If multiple values are supported (i.e. list of values), then accept an empty string: The header may
            // be added multiple times to the request/response message. E.g.
            //  Accept: text/xml; q=1
            //  Accept:
            //  Accept: text/plain; q=0.2
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return SupportsMultipleValues;
            }

            var separatorFound = false;
            var current = GetNextNonEmptyOrWhitespaceIndex(value, index, SupportsMultipleValues, out separatorFound);

            if (separatorFound && !SupportsMultipleValues)
            {
                return false; // leading separators not allowed if we don't support multiple values.
            }

            if (current == value.Length)
            {
                if (SupportsMultipleValues)
                {
                    index = current;
                }
                return SupportsMultipleValues;
            }

            CookieHeaderValue result = null;
            int length = CookieHeaderValue.GetCookieLength(value, current, out result);

            if (length == 0)
            {
                return false;
            }

            current = current + length;
            current = GetNextNonEmptyOrWhitespaceIndex(value, current, SupportsMultipleValues, out separatorFound);

            // If we support multiple values and we've not reached the end of the string, then we must have a separator.
            if ((separatorFound && !SupportsMultipleValues) || (!separatorFound && (current < value.Length)))
            {
                return false;
            }

            index = current;
            parsedValue = result;
            return true;
        }

        private static int GetNextNonEmptyOrWhitespaceIndex(string input, int startIndex, bool skipEmptyValues, out bool separatorFound)
        {
            Contract.Requires(input != null);
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
