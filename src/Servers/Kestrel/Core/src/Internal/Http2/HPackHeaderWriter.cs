// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal static class HPackHeaderWriter
    {
        /// <summary>
        /// Begin encoding headers in the first HEADERS frame.
        /// </summary>
        public static bool BeginEncodeHeaders(int statusCode, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
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
        public static bool BeginEncodeHeaders(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
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
        public static bool ContinueEncodeHeaders(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            return EncodeHeaders(headersEnumerator, buffer, throwIfNoneEncoded: true, out length);
        }

        private static bool EncodeHeaders(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, out int length)
        {
            var currentLength = 0;
            do
            {
                if (!EncodeHeader(headersEnumerator.KnownHeaderType, headersEnumerator.Current.Key, headersEnumerator.Current.Value, buffer.Slice(currentLength), out int headerLength))
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

        private static bool EncodeHeader(KnownHeaderType knownHeaderType, string name, string value, Span<byte> buffer, out int length)
        {
            var hPackStaticTableId = GetResponseHeaderStaticTableId(knownHeaderType);

            if (hPackStaticTableId == -1)
            {
                return HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, buffer, out length);
            }
            else
            {
                return HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(hPackStaticTableId, value, buffer, out length);
            }
        }

        private static int GetResponseHeaderStaticTableId(KnownHeaderType responseHeaderType)
        {
            switch (responseHeaderType)
            {
                case KnownHeaderType.CacheControl:
                    return H2StaticTable.CacheControl;
                case KnownHeaderType.Date:
                    return H2StaticTable.Date;
                case KnownHeaderType.TransferEncoding:
                    return H2StaticTable.TransferEncoding;
                case KnownHeaderType.Via:
                    return H2StaticTable.Via;
                case KnownHeaderType.Allow:
                    return H2StaticTable.Allow;
                case KnownHeaderType.ContentType:
                    return H2StaticTable.ContentType;
                case KnownHeaderType.ContentEncoding:
                    return H2StaticTable.ContentEncoding;
                case KnownHeaderType.ContentLanguage:
                    return H2StaticTable.ContentLanguage;
                case KnownHeaderType.ContentLocation:
                    return H2StaticTable.ContentLocation;
                case KnownHeaderType.ContentRange:
                    return H2StaticTable.ContentRange;
                case KnownHeaderType.Expires:
                    return H2StaticTable.Expires;
                case KnownHeaderType.LastModified:
                    return H2StaticTable.LastModified;
                case KnownHeaderType.AcceptRanges:
                    return H2StaticTable.AcceptRanges;
                case KnownHeaderType.Age:
                    return H2StaticTable.Age;
                case KnownHeaderType.ETag:
                    return H2StaticTable.ETag;
                case KnownHeaderType.Location:
                    return H2StaticTable.Location;
                case KnownHeaderType.ProxyAuthenticate:
                    return H2StaticTable.ProxyAuthenticate;
                case KnownHeaderType.RetryAfter:
                    return H2StaticTable.RetryAfter;
                case KnownHeaderType.Server:
                    return H2StaticTable.Server;
                case KnownHeaderType.SetCookie:
                    return H2StaticTable.SetCookie;
                case KnownHeaderType.Vary:
                    return H2StaticTable.Vary;
                case KnownHeaderType.WWWAuthenticate:
                    return H2StaticTable.WwwAuthenticate;
                case KnownHeaderType.AccessControlAllowOrigin:
                    return H2StaticTable.AccessControlAllowOrigin;
                case KnownHeaderType.ContentLength:
                    return H2StaticTable.ContentLength;
                default:
                    return -1;
            }
        }
    }
}
