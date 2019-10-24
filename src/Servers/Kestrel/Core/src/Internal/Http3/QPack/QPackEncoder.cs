// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal class QPackEncoder
    {
        private IEnumerator<KeyValuePair<string, string>> _enumerator;

        // TODO these all need to be updated!

        /*
         *      0   1   2   3   4   5   6   7
               +---+---+---+---+---+---+---+---+
               | 1 | S |      Index (6+)       |
               +---+---+-----------------------+
         */
        public static bool EncodeIndexedHeaderField(int index, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                EncodeHeaderBlockPrefix(destination, out bytesWritten);
                destination = destination.Slice(bytesWritten);

                return IntegerEncoder.Encode(index, 6, destination, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeIndexHeaderFieldWithPostBaseIndex(int index, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes a "Literal Header Field without Indexing".</summary>
        public static bool EncodeLiteralHeaderFieldWithNameReference(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length != 0)
            {
                EncodeHeaderBlockPrefix(destination, out bytesWritten);
                destination = destination.Slice(bytesWritten);

                return IntegerEncoder.Encode(index, 6, destination, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        /*
         *         0   1   2   3   4   5   6   7
                  +---+---+---+---+---+---+---+---+
                  | 0 | 1 | N | S |Name Index (4+)|
                  +---+---+---+---+---------------+
                  | H |     Value Length (7+)     |
                  +---+---------------------------+
                  |  Value String (Length bytes)  |
                  +-------------------------------+
         */
        public static bool EncodeLiteralHeaderFieldWithPostBaseNameReference(int index, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }

        public static bool EncodeLiteralHeaderFieldWithoutNameReference(int index, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }

        /*
         *     0   1   2   3   4   5   6   7
               +---+---+---+---+---+---+---+---+
               |   Required Insert Count (8+)  |
               +---+---------------------------+
               | S |      Delta Base (7+)      |
               +---+---------------------------+
               |      Compressed Headers     ...
               +-------------------------------+
         *
         */
        private static bool EncodeHeaderBlockPrefix(Span<byte> destination, out int bytesWritten)
        {
            int length;
            bytesWritten = 0;
            // Required insert count as first int
            if (!IntegerEncoder.Encode(0, 8, destination, out length))
            {
                return false;
            }

            bytesWritten += length;
            destination = destination.Slice(length);

            // Delta base
            if (destination.Length == 0)
            {
                return false;
            }

            destination[0] = 0x00; // zero out as we don't have a signed bit.
            if (!IntegerEncoder.Encode(0, 7, destination, out length))
            {
                return false;
            }

            bytesWritten += length;

            return true;
        }

        private static bool EncodeLiteralHeaderName(string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-5.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | H |    String Length (7+)     |
            // +---+---------------------------+
            // |  String Data (Length octets)  |
            // +-------------------------------+

            if (destination.Length != 0)
            {
                destination[0] = 0; // TODO: Use Huffman encoding
                if (IntegerEncoder.Encode(value.Length, 7, destination, out int integerLength))
                {
                    Debug.Assert(integerLength >= 1);

                    destination = destination.Slice(integerLength);
                    if (value.Length <= destination.Length)
                    {
                        for (int i = 0; i < value.Length; i++)
                        {
                            char c = value[i];
                            destination[i] = (byte)((uint)(c - 'A') <= ('Z' - 'A') ? c | 0x20 : c);
                        }

                        bytesWritten = integerLength + value.Length;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        private static bool EncodeStringLiteralValue(string value, Span<byte> destination, out int bytesWritten)
        {
            if (value.Length <= destination.Length)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if ((c & 0xFF80) != 0)
                    {
                        throw new HttpRequestException("");
                    }

                    destination[i] = (byte)c;
                }

                bytesWritten = value.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeStringLiteral(string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-5.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | H |    String Length (7+)     |
            // +---+---------------------------+
            // |  String Data (Length octets)  |
            // +-------------------------------+

            if (destination.Length != 0)
            {
                destination[0] = 0; // TODO: Use Huffman encoding
                if (IntegerEncoder.Encode(value.Length, 7, destination, out int integerLength))
                {
                    Debug.Assert(integerLength >= 1);

                    if (EncodeStringLiteralValue(value, destination.Slice(integerLength), out int valueLength))
                    {
                        bytesWritten = integerLength + valueLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeStringLiterals(string[] values, string separator, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (values.Length == 0)
            {
                return EncodeStringLiteral("", destination, out bytesWritten);
            }
            else if (values.Length == 1)
            {
                return EncodeStringLiteral(values[0], destination, out bytesWritten);
            }

            if (destination.Length != 0)
            {
                int valueLength = 0;

                // Calculate length of all parts and separators.
                foreach (string part in values)
                {
                    valueLength = checked((int)(valueLength + part.Length));
                }

                valueLength = checked((int)(valueLength + (values.Length - 1) * separator.Length));

                if (IntegerEncoder.Encode(valueLength, 7, destination, out int integerLength))
                {
                    Debug.Assert(integerLength >= 1);

                    int encodedLength = 0;
                    for (int j = 0; j < values.Length; j++)
                    {
                        if (j != 0 && !EncodeStringLiteralValue(separator, destination.Slice(integerLength), out encodedLength))
                        {
                            return false;
                        }

                        integerLength += encodedLength;

                        if (!EncodeStringLiteralValue(values[j], destination.Slice(integerLength), out encodedLength))
                        {
                            return false;
                        }

                        integerLength += encodedLength;
                    }

                    bytesWritten = integerLength;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing" to a new array, but only the index portion;
        /// a subsequent call to <see cref="EncodeStringLiteral"/> must be used to encode the associated value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(int index)
        {
            Span<byte> span = stackalloc byte[256];
            bool success = EncodeLiteralHeaderFieldWithPostBaseNameReference(index, span, out int length);
            Debug.Assert(success, $"Stack-allocated space was too small for index '{index}'.");
            return span.Slice(0, length).ToArray();
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing - New Name" to a new array, but only the name portion;
        /// a subsequent call to <see cref="EncodeStringLiteral"/> must be used to encode the associated value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutIndexingNewNameToAllocatedArray(string name)
        {
            Span<byte> span = stackalloc byte[256];
            bool success = EncodeLiteralHeaderFieldWithoutIndexingNewName(name, span, out int length);
            Debug.Assert(success, $"Stack-allocated space was too small for \"{name}\".");
            return span.Slice(0, length).ToArray();
        }

        private static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, Span<byte> span, out int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>Encodes a "Literal Header Field without Indexing" to a new array.</summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(int index, string value)
        {
            Span<byte> span =
#if DEBUG
                stackalloc byte[4]; // to validate growth algorithm
#else
                stackalloc byte[512];
#endif
            while (true)
            {
                if (EncodeLiteralHeaderFieldWithNameReference(index, value, span, out int length))
                {
                    return span.Slice(0, length).ToArray();
                }

                // This is a rare path, only used once per HTTP/2 connection and only
                // for very long host names.  Just allocate rather than complicate
                // the code with ArrayPool usage.  In practice we should never hit this,
                // as hostnames should be <= 255 characters.
                span = new byte[span.Length * 2];
            }
        }

        // TODO these are fairly hard coded for the first two bytes to be zero.
        public bool BeginEncode(IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length)
        {
            _enumerator = headers.GetEnumerator();
            _enumerator.MoveNext();
            buffer[0] = 0;
            buffer[1] = 0;

            return Encode(buffer.Slice(2), out length);
        }

        public bool BeginEncode(int statusCode, IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length)
        {
            _enumerator = headers.GetEnumerator();
            _enumerator.MoveNext();

            // https://quicwg.org/base-drafts/draft-ietf-quic-qpack.html#header-prefix
            buffer[0] = 0;
            buffer[1] = 0;

            var statusCodeLength = EncodeStatusCode(statusCode, buffer.Slice(2));
            var done = Encode(buffer.Slice(statusCodeLength + 2), throwIfNoneEncoded: false, out var headersLength);
            length = statusCodeLength + headersLength + 2;

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
                        throw new QPackEncodingException("TODO sync with corefx" /* CoreStrings.HPackErrorNotEnoughBuffer */);
                    }
                    return false;
                }

                length += headerLength;
            } while (_enumerator.MoveNext());

            return true;
        }

        private bool EncodeHeader(string name, string value, Span<byte> buffer, out int length)
        {
            var i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            if (!EncodeNameString(name, buffer.Slice(i), out var nameLength, lowercase: true))
            {
                return false;
            }

            i += nameLength;

            if (i >= buffer.Length)
            {
                return false;
            }

            if (!EncodeValueString(value, buffer.Slice(i), out var valueLength, lowercase: false))
            {
                return false;
            }

            i += valueLength;

            length = i;
            return true;
        }

        private bool EncodeValueString(string s, Span<byte> buffer, out int length, bool lowercase)
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

        private bool EncodeNameString(string s, Span<byte> buffer, out int length, bool lowercase)
        {
            const int toLowerMask = 0x20;

            var i = 0;
            length = 0;

            if (buffer.Length == 0)
            {
                return false;
            }

            buffer[0] = 0x30;

            if (!IntegerEncoder.Encode(s.Length, 3, buffer, out var nameLength))
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
                    // TODO this isn't safe, some index can be larger than 64. Encoded here!
                    buffer[0] = (byte)(0xC0 | StaticTable.Instance.StatusIndex[statusCode]);
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
    }
}
