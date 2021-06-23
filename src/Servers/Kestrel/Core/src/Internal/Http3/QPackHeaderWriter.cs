// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.Http.QPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal static class QPackHeaderWriter
    {
        public static bool BeginEncode(Http3HeadersEnumerator enumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
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

        public static bool BeginEncode(int statusCode, Http3HeadersEnumerator enumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
        {
            bool hasValue = enumerator.MoveNext();
            Debug.Assert(hasValue == true);

            // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#header-prefix
            buffer[0] = 0;
            buffer[1] = 0;

            int statusCodeLength = EncodeStatusCode(statusCode, buffer.Slice(2));
            totalHeaderSize += 42; // name (:status) + value (xxx) + overhead (32)

            bool done = Encode(enumerator, buffer.Slice(statusCodeLength + 2), throwIfNoneEncoded: false, ref totalHeaderSize, out int headersLength);
            length = statusCodeLength + headersLength + 2;

            return done;
        }

        public static bool Encode(Http3HeadersEnumerator enumerator, Span<byte> buffer, ref int totalHeaderSize, out int length)
        {
            return Encode(enumerator, buffer, throwIfNoneEncoded: true, ref totalHeaderSize, out length);
        }

        private static bool Encode(Http3HeadersEnumerator enumerator, Span<byte> buffer, bool throwIfNoneEncoded, ref int totalHeaderSize, out int length)
        {
            length = 0;

            do
            {
                var current = enumerator.Current;
                var valueEncoding = ReferenceEquals(enumerator.EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                    ? null : enumerator.EncodingSelector(current.Key);

                if (!QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReference(current.Key, current.Value, valueEncoding, buffer.Slice(length), out int headerLength))
                {
                    if (length == 0 && throwIfNoneEncoded)
                    {
                        throw new QPackEncodingException("TODO sync with corefx" /* CoreStrings.HPackErrorNotEnoughBuffer */);
                    }
                    return false;
                }

                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.3
                totalHeaderSize += HeaderField.GetLength(current.Key.Length, current.Value.Length);
                length += headerLength;
            } while (enumerator.MoveNext());

            return true;
        }

        private static int EncodeStatusCode(int statusCode, Span<byte> buffer)
        {
            switch (statusCode)
            {
                case 200:
                case 204:
                case 206:
                case 304:
                case 400:
                case 404:
                case 500:
                    QPackEncoder.EncodeStaticIndexedHeaderField(H3StaticTable.StatusIndex[statusCode], buffer, out var bytesWritten);
                    return bytesWritten;
                default:
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
}
