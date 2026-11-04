// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Http2Cat;

internal static class HPackHeaderWriter
{
    /// <summary>
    /// Begin encoding headers in the first HEADERS frame.
    /// </summary>
    public static bool BeginEncodeHeaders(int statusCode, IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, out int length)
    {
        if (!HPackEncoder.EncodeStatusHeader(statusCode, buffer, out var statusCodeLength))
        {
            throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
        }

        if (!headersEnumerator.MoveNext())
        {
            length = statusCodeLength;
            return true;
        }

        // We're ok with not throwing if no headers were encoded because we've already encoded the status.
        // There is a small chance that the header will encode if there is no other content in the next HEADERS frame.
        var done = EncodeHeaders(headersEnumerator, buffer.Slice(statusCodeLength), throwIfNoneEncoded: false, out var headersLength);
        length = statusCodeLength + headersLength;

        return done;
    }

    /// <summary>
    /// Begin encoding headers in the first HEADERS frame.
    /// </summary>
    public static bool BeginEncodeHeaders(IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, out int length)
    {
        if (!headersEnumerator.MoveNext())
        {
            length = 0;
            return true;
        }

        return EncodeHeaders(headersEnumerator, buffer, throwIfNoneEncoded: true, out length);
    }

    /// <summary>
    /// Continue encoding headers in the next HEADERS frame. The enumerator should already have a current value.
    /// </summary>
    public static bool ContinueEncodeHeaders(IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, out int length)
    {
        return EncodeHeaders(headersEnumerator, buffer, throwIfNoneEncoded: true, out length);
    }

    private static bool EncodeHeaders(IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, out int length)
    {
        var currentLength = 0;
        do
        {
            if (!EncodeHeader(headersEnumerator.Current.Key, headersEnumerator.Current.Value, buffer.Slice(currentLength), out int headerLength))
            {
                // The the header wasn't written and no headers have been written then the header is too large.
                // Throw an error to avoid an infinite loop of attempting to write large header.
                if (currentLength == 0 && throwIfNoneEncoded)
                {
                    throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
                }

                length = currentLength;
                return false;
            }

            currentLength += headerLength;
        }
        while (headersEnumerator.MoveNext());

        length = currentLength;

        return true;
    }

    private static bool EncodeHeader(string name, string value, Span<byte> buffer, out int length)
    {
        return HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, valueEncoding: null, buffer, out length);
    }
}
