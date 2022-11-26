// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Http;

internal static class HttpCharacters
{
    // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1
    private const string AlphaNumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    // Authority https://tools.ietf.org/html/rfc3986#section-3.2
    // Examples:
    // microsoft.com
    // hostname:8080
    // [::]:8080
    // [fe80::]
    // 127.0.0.1
    // user@host.com
    // user:password@host.com
    private static readonly IndexOfAnyValues<byte> _allowedAuthorityBytes = IndexOfAnyValues.Create(":.-[]@0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

    // Matches Http.Sys
    // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys
    private static readonly IndexOfAnyValues<char> _allowedHostChars = IndexOfAnyValues.Create("!$&'()-._~" + AlphaNumeric);

    // tchar https://tools.ietf.org/html/rfc7230#appendix-B
    private static readonly IndexOfAnyValues<char> _allowedTokenChars = IndexOfAnyValues.Create("!#$%&'*+-.^_`|~" + AlphaNumeric);
    private static readonly IndexOfAnyValues<byte> _allowedTokenBytes = IndexOfAnyValues.Create("!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

    // field-value https://tools.ietf.org/html/rfc7230#section-3.2
    private static readonly IndexOfAnyValues<char> _allowedFieldChars = CreateAllowedFieldChars();

    private static IndexOfAnyValues<char> CreateAllowedFieldChars()
    {
        // field-value https://tools.ietf.org/html/rfc7230#section-3.2

        Span<char> tmp = stackalloc char[128];
        tmp[0] = (char)0x9;     // HTAB
        var count = 1;

        for (var c = 0x20; c <= 0x7E; ++c)  // VCHAR and SP
        {
            tmp[count++] = (char)c;
        }

        Debug.Assert(count <= tmp.Length);
        return IndexOfAnyValues.Create(tmp[..count]);
    }

    public static bool ContainsInvalidAuthorityChar(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(_allowedAuthorityBytes) >= 0;

    public static int IndexOfInvalidHostChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedHostChars);

    public static int IndexOfInvalidTokenChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedTokenChars);

    public static int IndexOfInvalidTokenChar(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(_allowedTokenBytes);

    // Follows field-value rules in https://tools.ietf.org/html/rfc7230#section-3.2
    // Disallows characters > 0x7E.
    public static int IndexOfInvalidFieldValueChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedFieldChars);

    // Follows field-value rules for chars <= 0x7F. Allows extended characters > 0x7F.
    public static int IndexOfInvalidFieldValueCharExtended(ReadOnlySpan<char> span)
    {
        var idx = span.IndexOfAnyExcept(_allowedFieldChars);

        return idx < 0 ? -1 : IndexOfInvalidFieldValueCharExtended(span, idx);
    }

    private static int IndexOfInvalidFieldValueCharExtended(ReadOnlySpan<char> span, int idx)
    {
        while (true)
        {
            if (span[idx] <= 0x7F)
            {
                return idx;
            }

            var tmpIdx = span.Slice(idx + 1).IndexOfAnyExcept(_allowedFieldChars);
            if (tmpIdx < 0)
            {
                return -1;
            }

            idx += 1 + tmpIdx;
        }
    }
}
