// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal static class HPackHeaderWriter
    {
        public static bool EncodeHeaders(int statusCode, IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, out int length)
        {
            if (!HPackEncoder.EncodeStatusHeader(statusCode, buffer, out var statusCodeLength))
            {
                throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
            }

            // We're ok with not throwing if no headers were encoded because we've already encoded the status.
            // There is a small chance that the header will encode if there is no other content in the next HEADERS frame.
            var done = EncodeHeaders(headersEnumerator, buffer.Slice(statusCodeLength), throwIfNoneEncoded: false, out var headersLength);
            length = statusCodeLength + headersLength;

            return done;
        }

        public static bool EncodeHeaders(IEnumerator<KeyValuePair<string, string>> headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, out int length)
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
            return HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, buffer, out length);
        }
    }
}
