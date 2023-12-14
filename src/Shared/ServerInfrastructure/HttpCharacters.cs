// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

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
    private static readonly SearchValues<byte> _allowedAuthorityBytes = SearchValues.Create(":.-[]@0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

    // Matches Http.Sys
    // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys
    private static readonly SearchValues<char> _allowedHostChars = SearchValues.Create("!$&'()-._~" + AlphaNumeric);

    // tchar https://tools.ietf.org/html/rfc7230#appendix-B
    private static readonly SearchValues<char> _allowedTokenChars = SearchValues.Create("!#$%&'*+-.^_`|~" + AlphaNumeric);
    private static readonly SearchValues<byte> _allowedTokenBytes = SearchValues.Create("!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

    // field-value https://tools.ietf.org/html/rfc7230#section-3.2
    // HTAB, [VCHAR, SP]
    private static readonly SearchValues<char> _allowedFieldChars = SearchValues.Create("\t !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" + AlphaNumeric);

    // Values are [0x00, 0x1F] without 0x09 (HTAB) and with 0x7F.
    private static readonly SearchValues<char> _invalidFieldChars = SearchValues.Create(
        "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u000A\u000B\u000C\u000D\u000E\u000F\u0010" +
        "\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F\u007F");

    public static bool ContainsInvalidAuthorityChar(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(_allowedAuthorityBytes) >= 0;

    public static int IndexOfInvalidHostChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedHostChars);

    public static int IndexOfInvalidTokenChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedTokenChars);

    public static int IndexOfInvalidTokenChar(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(_allowedTokenBytes);

    // Follows field-value rules in https://tools.ietf.org/html/rfc7230#section-3.2
    // Disallows characters > 0x7E.
    public static int IndexOfInvalidFieldValueChar(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(_allowedFieldChars);

    // Follows field-value rules for chars <= 0x7F. Allows extended characters > 0x7F.
    public static int IndexOfInvalidFieldValueCharExtended(ReadOnlySpan<char> span) => span.IndexOfAny(_invalidFieldChars);
}
