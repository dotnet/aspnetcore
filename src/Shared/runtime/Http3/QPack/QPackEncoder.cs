// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.HPack;
using System.Text;

namespace System.Net.Http.QPack
{
    internal static class QPackEncoder
    {
        // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.5.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 1 | T |      Index (6+)       |
        // +---+---+-----------------------+
        //
        // Note for this method's implementation of above:
        // - T is constant 1 here, indicating a static table reference.
        public static bool EncodeStaticIndexedHeaderField(int index, Span<byte> destination, out int bytesWritten)
        {
            if (!destination.IsEmpty)
            {
                destination[0] = 0b11000000;
                return IntegerEncoder.Encode(index, 6, destination, out bytesWritten);
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        public static byte[] EncodeStaticIndexedHeaderFieldToArray(int index)
        {
            Span<byte> buffer = stackalloc byte[IntegerEncoder.MaxInt32EncodedLength];

            bool res = EncodeStaticIndexedHeaderField(index, buffer, out int bytesWritten);
            Debug.Assert(res);

            return buffer.Slice(0, bytesWritten).ToArray();
        }

        // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.5.4
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 1 | N | T |Name Index (4+)|
        // +---+---+---+---+---------------+
        // | H |     Value Length (7+)     |
        // +---+---------------------------+
        // |  Value String (Length bytes)  |
        // +-------------------------------+
        //
        // Note for this method's implementation of above:
        // - N is constant 0 here, indicating intermediates (proxies) can compress the header when fordwarding.
        // - T is constant 1 here, indicating a static table reference.
        // - H is constant 0 here, as we do not yet perform Huffman coding.
        public static bool EncodeLiteralHeaderFieldWithStaticNameReference(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            return EncodeLiteralHeaderFieldWithStaticNameReference(index, value, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeLiteralHeaderFieldWithStaticNameReference(int index, string value, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
        {
            // Requires at least two bytes (one for name reference header, one for value length)
            if (destination.Length >= 2)
            {
                destination[0] = 0b01010000;
                if (IntegerEncoder.Encode(index, 4, destination, out int headerBytesWritten))
                {
                    destination = destination.Slice(headerBytesWritten);

                    if (EncodeValueString(value, valueEncoding, destination, out int valueBytesWritten))
                    {
                        bytesWritten = headerBytesWritten + valueBytesWritten;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes just the name part of a Literal Header Field With Static Name Reference. Must call <see cref="EncodeValueString(string, Encoding?, Span{byte}, out int)"/> after to encode the header's value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(int index)
        {
            Span<byte> temp = stackalloc byte[IntegerEncoder.MaxInt32EncodedLength];

            temp[0] = 0b01110000;
            bool res = IntegerEncoder.Encode(index, 4, temp, out int headerBytesWritten);
            Debug.Assert(res);

            return temp.Slice(0, headerBytesWritten).ToArray();
        }

        public static byte[] EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(int index, string value)
        {
            Span<byte> temp = value.Length < 256 ? stackalloc byte[256 + IntegerEncoder.MaxInt32EncodedLength * 2] : new byte[value.Length + IntegerEncoder.MaxInt32EncodedLength * 2];
            bool res = EncodeLiteralHeaderFieldWithStaticNameReference(index, value, temp, out int bytesWritten);
            Debug.Assert(res);
            return temp.Slice(0, bytesWritten).ToArray();
        }

        // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#section-4.5.6
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 1 | N | H |NameLen(3+)|
        // +---+---+---+---+---+-----------+
        // |  Name String (Length bytes)   |
        // +---+---------------------------+
        // | H |     Value Length (7+)     |
        // +---+---------------------------+
        // |  Value String (Length bytes)  |
        // +-------------------------------+
        //
        // Note for this method's implementation of above:
        // - N is constant 0 here, indicating intermediates (proxies) can compress the header when fordwarding.
        // - H is constant 0 here, as we do not yet perform Huffman coding.
        public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, string value, Span<byte> destination, out int bytesWritten)
        {
            return EncodeLiteralHeaderFieldWithoutNameReference(name, value, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, string value, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
        {
            if (EncodeNameString(name, destination, out int nameLength) && EncodeValueString(value, valueEncoding, destination.Slice(nameLength), out int valueLength))
            {
                bytesWritten = nameLength + valueLength;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <summary>
        /// Encodes a Literal Header Field Without Name Reference, building the value by concatenating a collection of strings with separators.
        /// </summary>
        public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, ReadOnlySpan<string> values, string valueSeparator, Span<byte> destination, out int bytesWritten)
        {
            return EncodeLiteralHeaderFieldWithoutNameReference(name, values, valueSeparator, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeLiteralHeaderFieldWithoutNameReference(string name, ReadOnlySpan<string> values, string valueSeparator, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
        {
            if (EncodeNameString(name, destination, out int nameLength) && EncodeValueString(values, valueSeparator, valueEncoding, destination.Slice(nameLength), out int valueLength))
            {
                bytesWritten = nameLength + valueLength;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes just the value part of a Literawl Header Field Without Static Name Reference. Must call <see cref="EncodeValueString(string, Encoding?, Span{byte}, out int)"/> after to encode the header's value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutNameReferenceToArray(string name)
        {
            Span<byte> temp = name.Length < 256 ? stackalloc byte[256 + IntegerEncoder.MaxInt32EncodedLength] : new byte[name.Length + IntegerEncoder.MaxInt32EncodedLength];

            bool res = EncodeNameString(name, temp, out int nameLength);
            Debug.Assert(res);

            return temp.Slice(0, nameLength).ToArray();
        }

        public static byte[] EncodeLiteralHeaderFieldWithoutNameReferenceToArray(string name, string value)
        {
            Span<byte> temp = (name.Length + value.Length) < 256 ? stackalloc byte[256 + IntegerEncoder.MaxInt32EncodedLength * 2] : new byte[name.Length + value.Length + IntegerEncoder.MaxInt32EncodedLength * 2];

            bool res = EncodeLiteralHeaderFieldWithoutNameReference(name, value, temp, out int bytesWritten);
            Debug.Assert(res);

            return temp.Slice(0, bytesWritten).ToArray();
        }

        private static bool EncodeValueString(string s, Encoding? valueEncoding, Span<byte> buffer, out int length)
        {
            if (buffer.Length != 0)
            {
                buffer[0] = 0;

                int encodedStringLength = valueEncoding is null || ReferenceEquals(valueEncoding, Encoding.Latin1)
                    ? s.Length
                    : valueEncoding.GetByteCount(s);

                if (IntegerEncoder.Encode(encodedStringLength, 7, buffer, out int nameLength))
                {
                    buffer = buffer.Slice(nameLength);
                    if (buffer.Length >= encodedStringLength)
                    {
                        if (valueEncoding is null)
                        {
                            EncodeValueStringPart(s, buffer);
                        }
                        else
                        {
                            int written = valueEncoding.GetBytes(s, buffer);
                            Debug.Assert(written == encodedStringLength);
                        }

                        length = nameLength + encodedStringLength;
                        return true;
                    }
                }
            }

            length = 0;
            return false;
        }

        /// <summary>
        /// Encodes a value by concatenating a collection of strings, separated by a separator string.
        /// </summary>
        public static bool EncodeValueString(ReadOnlySpan<string> values, string? separator, Span<byte> buffer, out int length)
        {
            return EncodeValueString(values, separator, valueEncoding: null, buffer, out length);
        }

        public static bool EncodeValueString(ReadOnlySpan<string> values, string? separator, Encoding? valueEncoding, Span<byte> buffer, out int length)
        {
            if (values.Length == 1)
            {
                return EncodeValueString(values[0], valueEncoding, buffer, out length);
            }

            if (values.Length == 0)
            {
                // TODO: this will be called with a string array from HttpHeaderCollection. Can we ever get a 0-length array from that? Assert if not.
                return EncodeValueString(string.Empty, valueEncoding: null, buffer, out length);
            }

            if (buffer.Length > 0)
            {
                Debug.Assert(separator != null);
                int valueLength;
                if (valueEncoding is null || ReferenceEquals(valueEncoding, Encoding.Latin1))
                {
                    valueLength = separator.Length * (values.Length - 1);
                    foreach (string part in values)
                    {
                        valueLength += part.Length;
                    }
                }
                else
                {
                    valueLength = valueEncoding.GetByteCount(separator) * (values.Length - 1);
                    foreach (string part in values)
                    {
                        valueLength += valueEncoding.GetByteCount(part);
                    }
                }

                buffer[0] = 0;
                if (IntegerEncoder.Encode(valueLength, 7, buffer, out int nameLength))
                {
                    buffer = buffer.Slice(nameLength);
                    if (buffer.Length >= valueLength)
                    {
                        if (valueEncoding is null)
                        {
                            string value = values[0];
                            EncodeValueStringPart(value, buffer);
                            buffer = buffer.Slice(value.Length);

                            for (int i = 1; i < values.Length; i++)
                            {
                                EncodeValueStringPart(separator, buffer);
                                buffer = buffer.Slice(separator.Length);

                                value = values[i];
                                EncodeValueStringPart(value, buffer);
                                buffer = buffer.Slice(value.Length);
                            }
                        }
                        else
                        {
                            int written = valueEncoding.GetBytes(values[0], buffer);
                            buffer = buffer.Slice(written);

                            for (int i = 1; i < values.Length; i++)
                            {
                                written = valueEncoding.GetBytes(separator, buffer);
                                buffer = buffer.Slice(written);

                                written = valueEncoding.GetBytes(values[i], buffer);
                                buffer = buffer.Slice(written);
                            }
                        }

                        length = nameLength + valueLength;
                        return true;
                    }
                }
            }

            length = 0;
            return false;
        }

        private static void EncodeValueStringPart(string s, Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= s.Length);

            for (int i = 0; i < s.Length; ++i)
            {
                char ch = s[i];

                if (ch > 127)
                {
                    throw new QPackEncodingException(SR.net_http_request_invalid_char_encoding);
                }

                buffer[i] = (byte)ch;
            }
        }

        private static bool EncodeNameString(string s, Span<byte> buffer, out int length)
        {
            const int toLowerMask = 0x20;

            if (buffer.Length != 0)
            {
                buffer[0] = 0x30;

                if (IntegerEncoder.Encode(s.Length, 3, buffer, out int nameLength))
                {
                    buffer = buffer.Slice(nameLength);

                    if (buffer.Length >= s.Length)
                    {
                        for (int i = 0; i < s.Length; ++i)
                        {
                            int ch = s[i];
                            Debug.Assert(ch <= 127, "HttpHeaders prevents adding non-ASCII header names.");

                            if ((uint)(ch - 'A') <= 'Z' - 'A')
                            {
                                ch |= toLowerMask;
                            }

                            buffer[i] = (byte)ch;
                        }

                        length = nameLength + s.Length;
                        return true;
                    }
                }
            }

            length = 0;
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
            if (destination.IsEmpty)
            {
                return false;
            }

            destination[0] = 0x00;
            if (!IntegerEncoder.Encode(0, 7, destination, out length))
            {
                return false;
            }

            bytesWritten += length;

            return true;
        }
    }
}
