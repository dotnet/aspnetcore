// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

internal abstract class BaseHeaderParser<T> : HttpHeaderParser<T>
{
    protected BaseHeaderParser(bool supportsMultipleValues)
        : base(supportsMultipleValues)
    {
    }

    // Returns the number of characters consumed at 'startIndex'.
    //   - returns > 0 and sets 'parsedValue' to a non-null value when a value was successfully parsed.
    //   - returns > 0 and sets 'parsedValue' to null when input was consumed but no value could be
    //     produced (e.g. an unterminated quoted-string was scanned to end-of-input). This lets the
    //     outer recovery loop in HttpHeaderParser.TryParseValues skip past the malformed span in O(1)
    //     instead of advancing one character at a time.
    //   - returns 0 when nothing was recognized at 'startIndex'.
    protected abstract int GetParsedValueLength(StringSegment value, int startIndex, out T? parsedValue);

    public sealed override bool TryParseValue(StringSegment value, int startIndex, out int parsedLength, out T? parsedValue)
    {
        parsedLength = 0;
        parsedValue = default;

        // If multiple values are supported (i.e. list of values), then accept an empty string: The header may
        // be added multiple times to the request/response message. E.g.
        //  Accept: text/xml; q=1
        //  Accept:
        //  Accept: text/plain; q=0.2
        if (StringSegment.IsNullOrEmpty(value) || (startIndex == value.Length))
        {
            return SupportsMultipleValues;
        }

        var current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, startIndex, SupportsMultipleValues,
            out var separatorFound);

        if (separatorFound && !SupportsMultipleValues)
        {
            return false; // leading separators not allowed if we don't support multiple values.
        }

        if (current == value.Length)
        {
            if (SupportsMultipleValues)
            {
                parsedLength = current - startIndex;
            }
            return SupportsMultipleValues;
        }

        var length = GetParsedValueLength(value, current, out var result);

        if (length == 0)
        {
            return false;
        }

        current = current + length;

        // The per-element parser consumed input but couldn't produce a value. Report the consumed
        // span so the caller can resume parsing after the malformed input rather than re-scanning it.
        if (result == null)
        {
            parsedLength = current - startIndex;
            return false;
        }

        current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, current, SupportsMultipleValues,
            out separatorFound);

        // If we support multiple values and we've not reached the end of the string, then we must have a separator.
        if ((separatorFound && !SupportsMultipleValues) || (!separatorFound && (current < value.Length)))
        {
            parsedLength = current - startIndex;
            return false;
        }

        parsedLength = current - startIndex;
        parsedValue = result;
        return true;
    }
}
