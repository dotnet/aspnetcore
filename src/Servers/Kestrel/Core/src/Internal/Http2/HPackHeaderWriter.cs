// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal static class HPackHeaderWriter
    {
        /// <summary>
        /// Begin encoding headers in the first HEADERS frame.
        /// </summary>
        public static bool BeginEncodeHeaders(int statusCode, HPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            length = 0;

            if (!hpackEncoder.EnsureDynamicTableSizeUpdate(buffer, out var sizeUpdateLength))
            {
                throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
            }
            length += sizeUpdateLength;

            if (!EncodeStatusHeader(statusCode, hpackEncoder, buffer.Slice(length), out var statusCodeLength))
            {
                throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
            }
            length += statusCodeLength;

            if (!headersEnumerator.MoveNext())
            {
                return true;
            }

            // We're ok with not throwing if no headers were encoded because we've already encoded the status.
            // There is a small chance that the header will encode if there is no other content in the next HEADERS frame.
            var done = EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer.Slice(length), throwIfNoneEncoded: false, out var headersLength);
            length += headersLength;
            return done;
        }

        /// <summary>
        /// Begin encoding headers in the first HEADERS frame.
        /// </summary>
        public static bool BeginEncodeHeaders(HPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            length = 0;

            if (!hpackEncoder.EnsureDynamicTableSizeUpdate(buffer, out var sizeUpdateLength))
            {
                throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
            }
            length += sizeUpdateLength;

            if (!headersEnumerator.MoveNext())
            {
                return true;
            }

            var done = EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer.Slice(length), throwIfNoneEncoded: true, out var headersLength);
            length += headersLength;
            return done;
        }

        /// <summary>
        /// Continue encoding headers in the next HEADERS frame. The enumerator should already have a current value.
        /// </summary>
        public static bool ContinueEncodeHeaders(HPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            return EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer, throwIfNoneEncoded: true, out length);
        }

        private static bool EncodeStatusHeader(int statusCode, HPackEncoder hpackEncoder, Span<byte> buffer, out int length)
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
                    // Status codes which exist in the HTTP/2 StaticTable.
                    return HPackEncoder.EncodeIndexedHeaderField(H2StaticTable.StatusIndex[statusCode], buffer, out length);
                default:
                    const string name = ":status";
                    var value = StatusCodes.ToStatusString(statusCode);
                    return hpackEncoder.EncodeHeader(buffer, H2StaticTable.Status200, HeaderEncodingHint.Index, name, value, out length);
            }
        }

        private static bool EncodeHeadersCore(HPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, out int length)
        {
            var currentLength = 0;
            do
            {
                var staticTableId = headersEnumerator.HPackStaticTableId;
                var name = headersEnumerator.Current.Key;
                var value = headersEnumerator.Current.Value;

                var hint = ResolveHeaderEncodingHint(staticTableId, name);

                if (!hpackEncoder.EncodeHeader(
                    buffer.Slice(currentLength),
                    staticTableId,
                    hint,
                    name,
                    value,
                    out var headerLength))
                {
                    // If the header wasn't written, and no headers have been written, then the header is too large.
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

        private static HeaderEncodingHint ResolveHeaderEncodingHint(int staticTableId, string name)
        {
            HeaderEncodingHint hint;
            if (IsSensitive(staticTableId, name))
            {
                hint = HeaderEncodingHint.NeverIndex;
            }
            else if (IsNotDynamicallyIndexed(staticTableId))
            {
                hint = HeaderEncodingHint.IgnoreIndex;
            }
            else
            {
                hint = HeaderEncodingHint.Index;
            }

            return hint;
        }

        private static bool IsSensitive(int staticTableIndex, string name)
        {
            // Set-Cookie could contain sensitive data.
            if (staticTableIndex == H2StaticTable.SetCookie)
            {
                return true;
            }
            if (string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsNotDynamicallyIndexed(int staticTableIndex)
        {
            // Content-Length is added to static content. Content length is different for each
            // file, and is unlikely to be reused because of browser caching.
            return staticTableIndex == H2StaticTable.ContentLength;
        }
    }
}
