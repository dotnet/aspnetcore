// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

internal static class HttpRuleParser
{
    // token = 1*<any CHAR except CTLs or separators>
    // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>
    private static readonly SearchValues<char> TokenChars =
        SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

    private const int MaxNestedCount = 5;
    private static readonly string[] DateFormats = new string[]
    {
        // "r", // RFC 1123, required output format but too strict for input
        "ddd, d MMM yyyy H:m:s 'GMT'", // RFC 1123 (r, except it allows both 1 and 01 for date and time)
        "ddd, d MMM yyyy H:m:s", // RFC 1123, no zone - assume GMT
        "d MMM yyyy H:m:s 'GMT'", // RFC 1123, no day-of-week
        "d MMM yyyy H:m:s", // RFC 1123, no day-of-week, no zone
        "ddd, d MMM yy H:m:s 'GMT'", // RFC 1123, short year
        "ddd, d MMM yy H:m:s", // RFC 1123, short year, no zone
        "d MMM yy H:m:s 'GMT'", // RFC 1123, no day-of-week, short year
        "d MMM yy H:m:s", // RFC 1123, no day-of-week, short year, no zone

        "dddd, d'-'MMM'-'yy H:m:s 'GMT'", // RFC 850, short year
        "dddd, d'-'MMM'-'yy H:m:s", // RFC 850 no zone
        "ddd, d'-'MMM'-'yyyy H:m:s 'GMT'", // RFC 850, long year
        "ddd MMM d H:m:s yyyy", // ANSI C's asctime() format

        "ddd, d MMM yyyy H:m:s zzz", // RFC 5322
        "ddd, d MMM yyyy H:m:s", // RFC 5322 no zone
        "d MMM yyyy H:m:s zzz", // RFC 5322 no day-of-week
        "d MMM yyyy H:m:s", // RFC 5322 no day-of-week, no zone
    };

    internal const char CR = '\r';
    internal const char LF = '\n';
    internal const char SP = ' ';
    internal const char Tab = '\t';
    internal const int MaxInt64Digits = 19;
    internal const int MaxInt32Digits = 10;

    // iso-8859-1, Western European (ISO)
    internal static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding("iso-8859-1");

    [Pure]
    internal static int GetTokenLength(StringSegment input, int startIndex)
    {
        Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

        if (startIndex >= input.Length)
        {
            return 0;
        }

        var subspan = input.AsSpan(startIndex);
        var firstNonTokenCharIdx = subspan.IndexOfAnyExcept(TokenChars);
        return (firstNonTokenCharIdx == -1) ? subspan.Length : firstNonTokenCharIdx;
    }

    internal static int GetWhitespaceLength(StringSegment input, int startIndex)
    {
        Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

        if (startIndex >= input.Length)
        {
            return 0;
        }

        var current = startIndex;

        char c;
        while (current < input.Length)
        {
            c = input[current];

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
                    var spaceOrTab = input[current + 2];
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

    internal static int GetNumberLength(StringSegment input, int startIndex, bool allowDecimal)
    {
        Contract.Requires((startIndex >= 0) && (startIndex < input.Length));
        Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

        var current = startIndex;
        char c;

        // If decimal values are not allowed, we pretend to have read the '.' character already. I.e. if a dot is
        // found in the string, parsing will be aborted.
        var haveDot = !allowDecimal;

        // The RFC doesn't allow decimal values starting with dot. I.e. value ".123" is invalid. It must be in the
        // form "0.123". Also, there are no negative values defined in the RFC. So we'll just parse non-negative
        // values.
        // The RFC only allows decimal dots not ',' characters as decimal separators. Therefore value "1,23" is
        // considered invalid and must be represented as "1.23".
        if (input[current] == '.')
        {
            return 0;
        }

        while (current < input.Length)
        {
            c = input[current];
            if ((c >= '0') && (c <= '9'))
            {
                current++;
            }
            else if (!haveDot && (c == '.'))
            {
                // Note that value "1." is valid.
                haveDot = true;
                current++;
            }
            else
            {
                break;
            }
        }

        return current - startIndex;
    }

    internal static HttpParseResult GetQuotedStringLength(StringSegment input, int startIndex, out int length)
    {
        var nestedCount = 0;
        return GetExpressionLength(input, startIndex, '"', '"', false, ref nestedCount, out length);
    }

    // quoted-pair = "\" CHAR
    // CHAR = <any US-ASCII character (octets 0 - 127)>
    internal static HttpParseResult GetQuotedPairLength(StringSegment input, int startIndex, out int length)
    {
        Contract.Requires((startIndex >= 0) && (startIndex < input.Length));
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

    // Try the various date formats in the order listed above.
    // We should accept a wide verity of common formats, but only output RFC 1123 style dates.
    internal static bool TryStringToDate(StringSegment input, out DateTimeOffset result)
    {
        ReadOnlySpan<char> inputSpan = input.AsSpan();
        DateTimeFormatInfo invariant = DateTimeFormatInfo.InvariantInfo;

        // RFC 1123 is most common and has a fast path in TryParseExact.  Try it first, and only if that
        // fails fall back to trying the rest of the formats.
        return
            DateTimeOffset.TryParseExact(inputSpan, "r", invariant, DateTimeStyles.None, out result) ||
            DateTimeOffset.TryParseExact(inputSpan, DateFormats, invariant, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out result);
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
        StringSegment input,
        int startIndex,
        char openChar,
        char closeChar,
        bool supportsNesting,
        ref int nestedCount,
        out int length)
    {
        Contract.Requires(input != null);
        Contract.Requires((startIndex >= 0) && (startIndex < input.Length));

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

                    var nestedLength = 0;
                    var nestedResult = GetExpressionLength(input, current, openChar, closeChar,
                        supportsNesting, ref nestedCount, out nestedLength);

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
