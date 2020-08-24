// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Net.Http.HPack
{
    internal partial class HPackEncoder
    {
        // Things we should add:
        // * Huffman encoding
        //
        // Things we should consider adding:
        // * Dynamic table encoding:
        //   This would make the encoder stateful, which complicates things significantly.
        //   Additionally, it's not clear exactly what strings we would add to the dynamic table
        //   without some additional guidance from the user about this.
        //   So for now, don't do dynamic encoding.

        /// <summary>Encodes an "Indexed Header Field".</summary>
        public static bool EncodeIndexedHeaderField(int index, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.1
            // ----------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 1 |        Index (7+)         |
            // +---+---------------------------+

            if (destination.Length != 0)
            {
                destination[0] = 0x80;
                return IntegerEncoder.Encode(index, 7, destination, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes the status code of a response to the :status field.</summary>
        public static bool EncodeStatusHeader(int statusCode, Span<byte> destination, out int bytesWritten)
        {
            // Bytes written depend on whether the status code value maps directly to an index
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
                    return EncodeIndexedHeaderField(H2StaticTable.GetStatusIndex(statusCode), destination, out bytesWritten);
                default:
                    // If the status code doesn't have a static index then we need to include the full value.
                    // Write a status index and then the number bytes as a string literal.
                    if (!EncodeLiteralHeaderFieldWithoutIndexing(H2StaticTable.Status200, destination, out var nameLength))
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    var statusBytes = StatusCodes.ToStatusBytes(statusCode);

                    if (!EncodeStringLiteral(statusBytes, destination.Slice(nameLength), out var valueLength))
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    bytesWritten = nameLength + valueLength;
                    return true;
            }
        }

        /// <summary>Encodes a "Literal Header Field without Indexing".</summary>
        public static bool EncodeLiteralHeaderFieldWithoutIndexing(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 0 |  Index (4+)   |
            // +---+---+-----------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length >= 2)
            {
                destination[0] = 0;
                if (IntegerEncoder.Encode(index, 4, destination, out int indexLength))
                {
                    Debug.Assert(indexLength >= 1);
                    if (EncodeStringLiteral(value, valueEncoding: null, destination.Slice(indexLength), out int nameLength))
                    {
                        bytesWritten = indexLength + nameLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes a "Literal Header Field never Indexing".</summary>
        public static bool EncodeLiteralHeaderFieldNeverIndexing(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.3
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 1 |  Index (4+)   |
            // +---+---+-----------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length >= 2)
            {
                destination[0] = 0x10;
                if (IntegerEncoder.Encode(index, 4, destination, out int indexLength))
                {
                    Debug.Assert(indexLength >= 1);
                    if (EncodeStringLiteral(value, valueEncoding: null, destination.Slice(indexLength), out int nameLength))
                    {
                        bytesWritten = indexLength + nameLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes a "Literal Header Field with Indexing".</summary>
        public static bool EncodeLiteralHeaderFieldIndexing(int index, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 1 |      Index (6+)       |
            // +---+---+-----------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length >= 2)
            {
                destination[0] = 0x40;
                if (IntegerEncoder.Encode(index, 6, destination, out int indexLength))
                {
                    Debug.Assert(indexLength >= 1);
                    if (EncodeStringLiteral(value, valueEncoding: null, destination.Slice(indexLength), out int nameLength))
                    {
                        bytesWritten = indexLength + nameLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing", but only the index portion;
        /// a subsequent call to <c>EncodeStringLiteral</c> must be used to encode the associated value.
        /// </summary>
        public static bool EncodeLiteralHeaderFieldWithoutIndexing(int index, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 0 |  Index (4+)   |
            // +---+---+-----------------------+
            //
            // ... expected after this:
            //
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length != 0)
            {
                destination[0] = 0;
                if (IntegerEncoder.Encode(index, 4, destination, out int indexLength))
                {
                    Debug.Assert(indexLength >= 1);
                    bytesWritten = indexLength;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes a "Literal Header Field with Indexing - New Name".</summary>
        public static bool EncodeLiteralHeaderFieldIndexingNewName(string name, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 1 |           0           |
            // +---+---+-----------------------+
            // | H |     Name Length (7+)      |
            // +---+---------------------------+
            // |  Name String (Length octets)  |
            // +---+---------------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            return EncodeLiteralHeaderNewNameCore(0x40, name, value, destination, out bytesWritten);
        }

        /// <summary>Encodes a "Literal Header Field without Indexing - New Name".</summary>
        public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 0 |       0       |
            // +---+---+-----------------------+
            // | H |     Name Length (7+)      |
            // +---+---------------------------+
            // |  Name String (Length octets)  |
            // +---+---------------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            return EncodeLiteralHeaderNewNameCore(0, name, value, destination, out bytesWritten);
        }

        /// <summary>Encodes a "Literal Header Field never Indexing - New Name".</summary>
        public static bool EncodeLiteralHeaderFieldNeverIndexingNewName(string name, string value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.3
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 1 |       0       |
            // +---+---+-----------------------+
            // | H |     Name Length (7+)      |
            // +---+---------------------------+
            // |  Name String (Length octets)  |
            // +---+---------------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            return EncodeLiteralHeaderNewNameCore(0x10, name, value, destination, out bytesWritten);
        }

        private static bool EncodeLiteralHeaderNewNameCore(byte mask, string name, string value, Span<byte> destination, out int bytesWritten)
        {
            if ((uint)destination.Length >= 3)
            {
                destination[0] = mask;
                if (EncodeLiteralHeaderName(name, destination.Slice(1), out int nameLength) &&
                    EncodeStringLiteral(value, valueEncoding: null, destination.Slice(1 + nameLength), out int valueLength))
                {
                    bytesWritten = 1 + nameLength + valueLength;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Encodes a "Literal Header Field without Indexing - New Name".</summary>
        public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, ReadOnlySpan<string> values, string separator, Span<byte> destination, out int bytesWritten)
        {
            return EncodeLiteralHeaderFieldWithoutIndexingNewName(name, values, separator, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, ReadOnlySpan<string> values, string separator, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 0 |       0       |
            // +---+---+-----------------------+
            // | H |     Name Length (7+)      |
            // +---+---------------------------+
            // |  Name String (Length octets)  |
            // +---+---------------------------+
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length >= 3)
            {
                destination[0] = 0;
                if (EncodeLiteralHeaderName(name, destination.Slice(1), out int nameLength) &&
                    EncodeStringLiterals(values, separator, valueEncoding, destination.Slice(1 + nameLength), out int valueLength))
                {
                    bytesWritten = 1 + nameLength + valueLength;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing - New Name", but only the name portion;
        /// a subsequent call to <c>EncodeStringLiteral</c> must be used to encode the associated value.
        /// </summary>
        public static bool EncodeLiteralHeaderFieldWithoutIndexingNewName(string name, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.2.2
            // ------------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 0 | 0 |       0       |
            // +---+---+-----------------------+
            // | H |     Name Length (7+)      |
            // +---+---------------------------+
            // |  Name String (Length octets)  |
            // +---+---------------------------+
            //
            // ... expected after this:
            //
            // | H |     Value Length (7+)     |
            // +---+---------------------------+
            // | Value String (Length octets)  |
            // +-------------------------------+

            if ((uint)destination.Length >= 2)
            {
                destination[0] = 0;
                if (EncodeLiteralHeaderName(name, destination.Slice(1), out int nameLength))
                {
                    bytesWritten = 1 + nameLength;
                    return true;
                }
            }

            bytesWritten = 0;
            return false;
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

        private static void EncodeValueStringPart(string value, Span<byte> destination)
        {
            Debug.Assert(destination.Length >= value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c & 0xFF80) != 0)
                {
                    throw new HttpRequestException(SR.net_http_request_invalid_char_encoding);
                }

                destination[i] = (byte)c;
            }
        }

        public static bool EncodeStringLiteral(ReadOnlySpan<byte> value, Span<byte> destination, out int bytesWritten)
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
                        // Note: No validation. Bytes should have already been validated.
                        value.CopyTo(destination);

                        bytesWritten = integerLength + value.Length;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeStringLiteral(string value, Span<byte> destination, out int bytesWritten)
        {
            return EncodeStringLiteral(value, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeStringLiteral(string value, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
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

                int encodedStringLength = valueEncoding is null || ReferenceEquals(valueEncoding, Encoding.Latin1)
                    ? value.Length
                    : valueEncoding.GetByteCount(value);

                if (IntegerEncoder.Encode(encodedStringLength, 7, destination, out int integerLength))
                {
                    Debug.Assert(integerLength >= 1);
                    destination = destination.Slice(integerLength);
                    if (encodedStringLength <= destination.Length)
                    {
                        if (valueEncoding is null)
                        {
                            EncodeValueStringPart(value, destination);
                        }
                        else
                        {
                            int written = valueEncoding.GetBytes(value, destination);
                            Debug.Assert(written == encodedStringLength);
                        }

                        bytesWritten = integerLength + encodedStringLength;
                        return true;
                    }
                }
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeDynamicTableSizeUpdate(int value, Span<byte> destination, out int bytesWritten)
        {
            // From https://tools.ietf.org/html/rfc7541#section-6.3
            // ----------------------------------------------------
            //   0   1   2   3   4   5   6   7
            // +---+---+---+---+---+---+---+---+
            // | 0 | 0 | 1 |   Max size (5+)   |
            // +---+---------------------------+

            if (destination.Length != 0)
            {
                destination[0] = 0x20;
                return IntegerEncoder.Encode(value, 5, destination, out bytesWritten);
            }

            bytesWritten = 0;
            return false;
        }

        public static bool EncodeStringLiterals(ReadOnlySpan<string> values, string? separator, Span<byte> destination, out int bytesWritten)
        {
            return EncodeStringLiterals(values, separator, valueEncoding: null, destination, out bytesWritten);
        }

        public static bool EncodeStringLiterals(ReadOnlySpan<string> values, string? separator, Encoding? valueEncoding, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (values.Length == 0)
            {
                return EncodeStringLiteral("", valueEncoding: null, destination, out bytesWritten);
            }
            else if (values.Length == 1)
            {
                return EncodeStringLiteral(values[0], valueEncoding, destination, out bytesWritten);
            }

            if (destination.Length != 0)
            {
                Debug.Assert(separator != null);
                int valueLength;

                // Calculate length of all parts and separators.
                if (valueEncoding is null || ReferenceEquals(valueEncoding, Encoding.Latin1))
                {
                    valueLength = checked((int)(values.Length - 1) * separator.Length);
                    foreach (string part in values)
                    {
                        valueLength = checked((int)(valueLength + part.Length));
                    }
                }
                else
                {
                    valueLength = checked((int)(values.Length - 1) * valueEncoding.GetByteCount(separator));
                    foreach (string part in values)
                    {
                        valueLength = checked((int)(valueLength + valueEncoding.GetByteCount(part)));
                    }
                }

                destination[0] = 0;
                if (IntegerEncoder.Encode(valueLength, 7, destination, out int integerLength))
                {
                    Debug.Assert(integerLength >= 1);
                    destination = destination.Slice(integerLength);
                    if (destination.Length >= valueLength)
                    {
                        if (valueEncoding is null)
                        {
                            string value = values[0];
                            EncodeValueStringPart(value, destination);
                            destination = destination.Slice(value.Length);

                            for (int i = 1; i < values.Length; i++)
                            {
                                EncodeValueStringPart(separator, destination);
                                destination = destination.Slice(separator.Length);

                                value = values[i];
                                EncodeValueStringPart(value, destination);
                                destination = destination.Slice(value.Length);
                            }
                        }
                        else
                        {
                            int written = valueEncoding.GetBytes(values[0], destination);
                            destination = destination.Slice(written);

                            for (int i = 1; i < values.Length; i++)
                            {
                                written = valueEncoding.GetBytes(separator, destination);
                                destination = destination.Slice(written);

                                written = valueEncoding.GetBytes(values[i], destination);
                                destination = destination.Slice(written);
                            }
                        }

                        bytesWritten = integerLength + valueLength;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing" to a new array, but only the index portion;
        /// a subsequent call to <c>EncodeStringLiteral</c> must be used to encode the associated value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(int index)
        {
            Span<byte> span = stackalloc byte[256];
            bool success = EncodeLiteralHeaderFieldWithoutIndexing(index, span, out int length);
            Debug.Assert(success, $"Stack-allocated space was too small for index '{index}'.");
            return span.Slice(0, length).ToArray();
        }

        /// <summary>
        /// Encodes a "Literal Header Field without Indexing - New Name" to a new array, but only the name portion;
        /// a subsequent call to <c>EncodeStringLiteral</c> must be used to encode the associated value.
        /// </summary>
        public static byte[] EncodeLiteralHeaderFieldWithoutIndexingNewNameToAllocatedArray(string name)
        {
            Span<byte> span = stackalloc byte[256];
            bool success = EncodeLiteralHeaderFieldWithoutIndexingNewName(name, span, out int length);
            Debug.Assert(success, $"Stack-allocated space was too small for \"{name}\".");
            return span.Slice(0, length).ToArray();
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
                if (EncodeLiteralHeaderFieldWithoutIndexing(index, value, span, out int length))
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
    }
}
