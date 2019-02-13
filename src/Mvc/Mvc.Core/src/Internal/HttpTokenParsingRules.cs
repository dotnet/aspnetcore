// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Text;

namespace Microsoft.AspNetCore.Mvc.Formatters.Internal
{
    public static class HttpTokenParsingRules
    {
        private static readonly bool[] TokenChars;
        private const int MaxNestedCount = 5;

        internal const char CR = '\r';
        internal const char LF = '\n';
        internal const char SP = ' ';
        internal const char Tab = '\t';
        internal const int MaxInt64Digits = 19;
        internal const int MaxInt32Digits = 10;

        // iso-8859-1, Western European (ISO)
        internal static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding(28591);

        static HttpTokenParsingRules()
        {
            // token = 1*<any CHAR except CTLs or separators>
            // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>

            TokenChars = new bool[128]; // everything is false

            for (int i = 33; i < 127; i++) // skip Space (32) & DEL (127)
            {
                TokenChars[i] = true;
            }

            // remove separators: these are not valid token characters
            TokenChars[(byte)'('] = false;
            TokenChars[(byte)')'] = false;
            TokenChars[(byte)'<'] = false;
            TokenChars[(byte)'>'] = false;
            TokenChars[(byte)'@'] = false;
            TokenChars[(byte)','] = false;
            TokenChars[(byte)';'] = false;
            TokenChars[(byte)':'] = false;
            TokenChars[(byte)'\\'] = false;
            TokenChars[(byte)'"'] = false;
            TokenChars[(byte)'/'] = false;
            TokenChars[(byte)'['] = false;
            TokenChars[(byte)']'] = false;
            TokenChars[(byte)'?'] = false;
            TokenChars[(byte)'='] = false;
            TokenChars[(byte)'{'] = false;
            TokenChars[(byte)'}'] = false;
        }

        internal static bool IsTokenChar(char character)
        {
            // Must be between 'space' (32) and 'DEL' (127)
            if (character > 127)
            {
                return false;
            }

            return TokenChars[character];
        }

        internal static int GetTokenLength(string input, int startIndex)
        {
            Contract.Requires(input != null);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

            if (startIndex >= input.Length)
            {
                return 0;
            }

            var current = startIndex;

            while (current < input.Length)
            {
                if (!IsTokenChar(input[current]))
                {
                    return current - startIndex;
                }
                current++;
            }
            return input.Length - startIndex;
        }

        internal static int GetWhitespaceLength(string input, int startIndex)
        {
            Contract.Requires(input != null);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

            if (startIndex >= input.Length)
            {
                return 0;
            }

            var current = startIndex;

            while (current < input.Length)
            {
                var c = input[current];

                if ((c == SP) || (c == Tab))
                {
                    current++;
                    continue;
                }

                if (c == CR)
                {
                    // If we have a #13 char, it must be followed by #10 and then at least one SP or HT.
                    if ((current + 2 < input.Length) && (input[current + 1] == LF))
                    {
                        char spaceOrTab = input[current + 2];
                        if ((spaceOrTab == SP) || (spaceOrTab == Tab))
                        {
                            current += 3;
                            continue;
                        }
                    }
                }

                return current - startIndex;
            }

            // All characters between startIndex and the end of the string are LWS characters.
            return input.Length - startIndex;
        }

        internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
        {
            var nestedCount = 0;
            return GetExpressionLength(input, startIndex, '"', '"', false, ref nestedCount, out length);
        }

        // quoted-pair = "\" CHAR
        // CHAR = <any US-ASCII character (octets 0 - 127)>
        internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
        {
            Contract.Requires(input != null);
            Contract.Requires((startIndex >= 0) && (startIndex < input.Length));
            Contract.Ensures((Contract.ValueAtReturn(out length) >= 0) &&
                (Contract.ValueAtReturn(out length) <= (input.Length - startIndex)));

            length = 0;

            if (input[startIndex] != '\\')
            {
                return HttpParseResult.NotParsed;
            }

            // Quoted-char has 2 characters. Check whether there are 2 chars left ('\' + char)
            // If so, check whether the character is in the range 0-127. If not, it's an invalid value.
            if ((startIndex + 2 > input.Length) || (input[startIndex + 1] > 127))
            {
                return HttpParseResult.InvalidFormat;
            }

            // We don't care what the char next to '\' is.
            length = 2;
            return HttpParseResult.Parsed;
        }

        // TEXT = <any OCTET except CTLs, but including LWS>
        // LWS = [CRLF] 1*( SP | HT )
        // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>
        //
        // Since we don't really care about the content of a quoted string or comment, we're more tolerant and
        // allow these characters. We only want to find the delimiters ('"' for quoted string and '(', ')' for comment).
        //
        // 'nestedCount': Comments can be nested. We allow a depth of up to 5 nested comments, i.e. something like
        // "(((((comment)))))". If we wouldn't define a limit an attacker could send a comment with hundreds of nested
        // comments, resulting in a stack overflow exception. In addition having more than 1 nested comment (if any)
        // is unusual.
        private static HttpParseResult GetExpressionLength(
            string input,
            int startIndex,
            char openChar,
            char closeChar,
            bool supportsNesting,
            ref int nestedCount,
            out int length)
        {
            Contract.Requires(input != null);
            Contract.Requires((startIndex >= 0) && (startIndex < input.Length));
            Contract.Ensures((Contract.Result<HttpParseResult>() != HttpParseResult.Parsed) ||
                (Contract.ValueAtReturn<int>(out length) > 0));

            length = 0;

            if (input[startIndex] != openChar)
            {
                return HttpParseResult.NotParsed;
            }

            var current = startIndex + 1; // Start parsing with the character next to the first open-char
            while (current < input.Length)
            {
                // Only check whether we have a quoted char, if we have at least 3 characters left to read (i.e.
                // quoted char + closing char). Otherwise the closing char may be considered part of the quoted char.
                if ((current + 2 < input.Length) &&
                    (GetQuotedPairLength(input, current, out var quotedPairLength) == HttpParseResult.Parsed))
                {
                    // We ignore invalid quoted-pairs. Invalid quoted-pairs may mean that it looked like a quoted pair,
                    // but we actually have a quoted-string: e.g. "\ü" ('\' followed by a char >127 - quoted-pair only
                    // allows ASCII chars after '\'; qdtext allows both '\' and >127 chars).
                    current = current + quotedPairLength;
                    continue;
                }

                // If we support nested expressions and we find an open-char, then parse the nested expressions.
                if (supportsNesting && (input[current] == openChar))
                {
                    nestedCount++;
                    try
                    {
                        // Check if we exceeded the number of nested calls.
                        if (nestedCount > MaxNestedCount)
                        {
                            return HttpParseResult.InvalidFormat;
                        }

                        var nestedResult = GetExpressionLength(
                            input,
                            current,
                            openChar,
                            closeChar,
                            supportsNesting,
                            ref nestedCount,
                            out var nestedLength);

                        switch (nestedResult)
                        {
                            case HttpParseResult.Parsed:
                                current += nestedLength; // add the length of the nested expression and continue.
                                break;

                            case HttpParseResult.NotParsed:
                                Contract.Assert(false, "'NotParsed' is unexpected: We started nested expression " +
                                    "parsing, because we found the open-char. So either it's a valid nested " +
                                    "expression or it has invalid format.");
                                break;

                            case HttpParseResult.InvalidFormat:
                                // If the nested expression is invalid, we can't continue, so we fail with invalid format.
                                return HttpParseResult.InvalidFormat;

                            default:
                                Contract.Assert(false, "Unknown enum result: " + nestedResult);
                                break;
                        }
                    }
                    finally
                    {
                        nestedCount--;
                    }
                }

                if (input[current] == closeChar)
                {
                    length = current - startIndex + 1;
                    return HttpParseResult.Parsed;
                }
                current++;
            }

            // We didn't see the final quote, therefore we have an invalid expression string.
            return HttpParseResult.InvalidFormat;
        }
    }
}
