// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.QPack;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal static class QPackHeaderWriter
{
    public static bool BeginEncodeHeaders(Http3HeadersEnumerator enumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
    {
        bool hasValue = enumerator.MoveNext();
        Debug.Assert(hasValue == true);

        buffer[0] = 0;
        buffer[1] = 0;

        bool doneEncode = Encode(enumerator, buffer.Slice(2), ref totalHeaderSize, out length);

        // Add two for the first two bytes.
        length += 2;
        return doneEncode;
    }

    public static bool BeginEncodeHeaders(int statusCode, Http3HeadersEnumerator headersEnumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
    {
        length = 0;

        // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#header-prefix
        buffer[0] = 0;
        buffer[1] = 0;

        int statusCodeLength = EncodeStatusCode(statusCode, buffer.Slice(2));
        totalHeaderSize += 42; // name (:status) + value (xxx) + overhead (32)
        length += statusCodeLength + 2;

        if (!headersEnumerator.MoveNext())
        {
            return true;
        }

        bool done = Encode(headersEnumerator, buffer.Slice(statusCodeLength + 2), throwIfNoneEncoded: false, ref totalHeaderSize, out int headersLength);
        length += headersLength;

        return done;
    }

    public static bool Encode(Http3HeadersEnumerator headersEnumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
    {
        return Encode(headersEnumerator, buffer, throwIfNoneEncoded: true, ref totalHeaderSize, out length);
    }

    private static bool Encode(Http3HeadersEnumerator headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, ref int totalHeaderSize, out int length)
    {
        length = 0;

        do
        {
            // Match the current header to the QPACK static table. Possible outcomes:
            // 1. Known header and value. Write index.
            // 2. Known header with custom value. Write name index and full value.
            // 3. Unknown header. Write full name and value.
            var (staticTableId, matchedValue) = headersEnumerator.GetQPackStaticTableId();
            var name = headersEnumerator.Current.Key;
            var value = headersEnumerator.Current.Value;

            int headerLength;
            if (matchedValue)
            {
                if (!QPackEncoder.EncodeStaticIndexedHeaderField(staticTableId, buffer.Slice(length), out headerLength))
                {
                    if (length == 0 && throwIfNoneEncoded)
                    {
                        throw new QPackEncodingException("TODO sync with corefx" /* CoreStrings.HPackErrorNotEnoughBuffer */);
                    }
                    return false;
                }
            }
            else
            {
                var valueEncoding = ReferenceEquals(headersEnumerator.EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                    ? null : headersEnumerator.EncodingSelector(name);

                if (!EncodeHeader(buffer.Slice(length), staticTableId, name, value, valueEncoding, out headerLength))
                {
                    if (length == 0 && throwIfNoneEncoded)
                    {
                        throw new QPackEncodingException("TODO sync with corefx" /* CoreStrings.HPackErrorNotEnoughBuffer */);
                    }
                    return false;
                }
            }

            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.3
            totalHeaderSize += HeaderField.GetLength(name.Length, value.Length);
            length += headerLength;
        } while (headersEnumerator.MoveNext());

        return true;
    }

    private static bool EncodeHeader(Span<byte> buffer, int staticTableId, string name, string value, Encoding? valueEncoding, out int headerLength)
    {
        return staticTableId == -1
            ? QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(name, value, valueEncoding, buffer, out headerLength)
            : QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReference(staticTableId, value, valueEncoding, buffer, out headerLength);
    }

    private static int EncodeStatusCode(int statusCode, Span<byte> buffer)
    {
        if (H3StaticTable.TryGetStatusIndex(statusCode, out var index))
        {
            QPackEncoder.EncodeStaticIndexedHeaderField(index, buffer, out var bytesWritten);
            return bytesWritten;
        }
        else
        {
            // https://tools.ietf.org/html/draft-ietf-quic-qpack-21#section-4.5.4
            // Index is 63 - :status
            buffer[0] = 0b01011111;
            buffer[1] = 0b00110000;

            ReadOnlySpan<byte> statusBytes = System.Net.Http.HPack.StatusCodes.ToStatusBytes(statusCode);
            buffer[2] = (byte)statusBytes.Length;
            statusBytes.CopyTo(buffer.Slice(3));

            return 3 + statusBytes.Length;
        }
    }
}
