// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
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
            int headerLength;
            if (headersEnumerator.TryGetCurrentContentLength(out var contentLength) &&
                ReferenceEquals(headersEnumerator.EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector))
            {
                // Fast path: write Content-Length digits directly without formatting a string.
                if (!EncodeContentLength(contentLength, buffer.Slice(length), out headerLength, out var valueLength))
                {
                    if (length == 0 && throwIfNoneEncoded)
                    {
                        throw new QPackEncodingException("TODO sync with corefx" /* CoreStrings.HPackErrorNotEnoughBuffer */);
                    }
                    return false;
                }

                totalHeaderSize += HeaderField.GetLength(Microsoft.Net.Http.Headers.HeaderNames.ContentLength.Length, valueLength);
                length += headerLength;
                continue;
            }

            // Match the current header to the QPACK static table. Possible outcomes:
            // 1. Known header and value. Write index.
            // 2. Known header with custom value. Write name index and full value.
            // 3. Unknown header. Write full name and value.
            var (staticTableId, matchedValue) = headersEnumerator.GetQPackStaticTableId();
            var name = headersEnumerator.Current.Key;
            var value = headersEnumerator.Current.Value;

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

    private static bool EncodeContentLength(long contentLength, Span<byte> buffer, out int headerLength, out int valueLength)
    {
        // Produces the same bytes as the general path: "content-length: 0" is in the static table, any other
        // value is a literal header field with a static name reference followed by the ASCII digits.
        if (contentLength == 0)
        {
            valueLength = 1;
            return QPackEncoder.EncodeStaticIndexedHeaderField(H3StaticTable.ContentLength0, buffer, out headerLength);
        }

        Span<byte> digits = stackalloc byte[20];
        var formatted = Utf8Formatter.TryFormat(contentLength, digits, out valueLength);
        Debug.Assert(formatted);

        // https://datatracker.ietf.org/doc/html/rfc9204#section-4.5.4
        if (buffer.Length >= 2)
        {
            buffer[0] = 0b01010000;
            if (System.Net.Http.HPack.IntegerEncoder.Encode(H3StaticTable.ContentLength0, 4, buffer, out var nameLength))
            {
                var valueBuffer = buffer.Slice(nameLength);
                if (valueBuffer.Length != 0)
                {
                    valueBuffer[0] = 0;
                    if (System.Net.Http.HPack.IntegerEncoder.Encode(valueLength, 7, valueBuffer, out var valueLengthLength) &&
                        digits.Slice(0, valueLength).TryCopyTo(valueBuffer.Slice(valueLengthLength)))
                    {
                        headerLength = nameLength + valueLengthLength + valueLength;
                        return true;
                    }
                }
            }
        }

        headerLength = 0;
        return false;
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
