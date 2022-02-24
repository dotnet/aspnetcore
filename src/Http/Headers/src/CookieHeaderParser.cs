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

    public override bool TryParseValue(StringSegment value, ref int index, out CookieHeaderValue? cookieValue)
    {
        cookieValue = null;

        if (!CookieHeaderParserShared.TryParseValue(value, ref index, SupportsMultipleValues, out var parsedName, out var parsedValue))
        {
            return false;
        }

        if (parsedName == null || parsedValue == null)
        {
            // Successfully parsed, but no values.
            return true;
        }

        cookieValue = new CookieHeaderValue(parsedName.Value, parsedValue.Value);

        return true;
    }
}
