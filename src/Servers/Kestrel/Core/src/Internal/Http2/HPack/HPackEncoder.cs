// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public class HPackEncoder
    {
        private IEnumerator<KeyValuePair<string, string>> _enumerator;

        public bool BeginEncode(IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length)
        {
            _enumerator = headers.GetEnumerator();
            _enumerator.MoveNext();

            return Encode(buffer, out length);
        }

        public bool BeginEncode(int statusCode, IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length)
        {
            _enumerator = headers.GetEnumerator();
            _enumerator.MoveNext();

            var statusCodeLength = EncodeStatusCode(statusCode, buffer);
            var done = Encode(buffer.Slice(statusCodeLength), throwIfNoneEncoded: false, out var headersLength);
            length = statusCodeLength + headersLength;

            return done;
        }

        public bool Encode(Span<byte> buffer, out int length)
        {
            return Encode(buffer, throwIfNoneEncoded: true, out length);
        }

        private bool Encode(Span<byte> buffer, bool throwIfNoneEncoded, out int length)
        {
            length = 0;

            do
            {
                if (!EncodeHeader(_enumerator.Current.Key, _enumerator.Current.Value, buffer.Slice(length), out var headerLength))
                {
                    if (length == 0 && throwIfNoneEncoded)
                    {
                        throw new HPackEncodingException(CoreStrings.HPackErrorNotEnoughBuffer);
                    }
                    return false;
                }

                length += headerLength;
            } while (_enumerator.MoveNext());

            return true;
        }

        private int EncodeStatusCode(int statusCode, Span<byte> buffer)
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
                    buffer[0] = (byte)(0x80 | StaticTable.Instance.StatusIndex[statusCode]);
                    return 1;
                default:
                    // Send as Literal Header Field Without Indexing - Indexed Name
                    buffer[0] = 0x08;

                    var statusBytes = StatusCodes.ToStatusBytes(statusCode);
                    buffer[1] = (byte)statusBytes.Length;
                    ((Span<byte>)statusBytes).CopyTo(buffer.Slice(2));

                    return 2 + statusBytes.Length;
            }
        }

        private bool EncodeHeader(string name, string value, Span<byte> buffer, out int length)
        {
            var i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[i++] = 0;

            if (i == buffer.Length)
            {
                return false;
            }

            if (!EncodeString(name, buffer.Slice(i), out var nameLength, lowercase: true))
            {
                return false;
            }

            i += nameLength;

            if (i >= buffer.Length)
            {
                return false;
            }

            if (!EncodeString(value, buffer.Slice(i), out var valueLength, lowercase: false))
            {
                return false;
            }

            i += valueLength;

            length = i;
            return true;
        }

        private bool EncodeString(string s, Span<byte> buffer, out int length, bool lowercase)
        {
            const int toLowerMask = 0x20;

            var i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0;

            if (!IntegerEncoder.Encode(s.Length, 7, buffer, out var nameLength))
            {
                return false;
            }

            i += nameLength;

            // TODO: use huffman encoding
            for (var j = 0; j < s.Length; j++)
            {
                if (i >= buffer.Length)
                {
                    return false;
                }

                buffer[i++] = (byte)(s[j] | (lowercase && s[j] >= (byte)'A' && s[j] <= (byte)'Z' ? toLowerMask : 0));
            }

            length = i;
            return true;
        }
    }
}
