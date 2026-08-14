// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

internal sealed class CookieHeaderParser : HttpHeaderParser<CookieHeaderValue>
{
    internal CookieHeaderParser(bool supportsMultipleValues)
        : base(supportsMultipleValues)
    {
    }

    public override bool TryParseValue(StringSegment value, int startIndex, out int parsedLength, out CookieHeaderValue? cookieValue)
    {
        cookieValue = null;
        var index = startIndex;

        if (!CookieHeaderParserShared.TryParseValue(value, ref index, SupportsMultipleValues, out var parsedName, out var parsedValue))
        {
            parsedLength = index - startIndex;
            return false;
        }

        parsedLength = index - startIndex;

        if (parsedName == null || parsedValue == null)
        {
            // Successfully consumed input, but no value to produce.
            return true;
        }

        cookieValue = new CookieHeaderValue(parsedName.Value, parsedValue.Value);

        return true;
    }
}
