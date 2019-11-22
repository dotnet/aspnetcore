// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.HPack;
using System.Text;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class HuffmanDecodingTests
    {
        // Encoded values are 30 bits at most, so are stored in the table in a uint.
        // Convert to ulong here and put the encoded value in the most significant bits.
        // This makes the encoding logic below simpler.
        private static (ulong code, int bitLength) GetEncodedValue(byte b)
        {
            (uint code, int bitLength) = Huffman.Encode(b);
            return (((ulong)code) << 32, bitLength);
        }

        private static int Encode(byte[] source, byte[] destination, bool injectEOS)
        {
            ulong currentBits = 0;  // We can have 7 bits of rollover plus 30 bits for the next encoded value, so use a ulong
            int currentBitCount = 0;
            int dstOffset = 0;

            for (int i = 0; i < source.Length; i++)
            {
                (ulong code, int bitLength) = GetEncodedValue(source[i]);

                // inject EOS if instructed to
                if (injectEOS)
                {
                    code |= (ulong)0b11111111_11111111_11111111_11111100 << (32 - bitLength);
                    bitLength += 30;
                    injectEOS = false;
                }

                currentBits |= code >> currentBitCount;
                currentBitCount += bitLength;

                while (currentBitCount >= 8)
                {
                    destination[dstOffset++] = (byte)(currentBits >> 56);
                    currentBits = currentBits << 8;
                    currentBitCount -= 8;
                }
            }

            // Fill any trailing bits with ones, per RFC
            if (currentBitCount > 0)
            {
                currentBits |= 0xFFFFFFFFFFFFFFFF >> currentBitCount;
                destination[dstOffset++] = (byte)(currentBits >> 56);
            }

            return dstOffset;
        }

        [Fact]
        public void HuffmanDecoding_ValidEncoding_Succeeds()
        {
            foreach (byte[] input in TestData())
            {
                // Worst case encoding is 30 bits per input byte, so make the encoded buffer 4 times as big
                byte[] encoded = new byte[input.Length * 4];
                int encodedByteCount = Encode(input, encoded, false);

                // Worst case decoding is an output byte per 5 input bits, so make the decoded buffer 2 times as big
                byte[] decoded = new byte[encoded.Length * 2];

                int decodedByteCount = Huffman.Decode(new ReadOnlySpan<byte>(encoded, 0, encodedByteCount), ref decoded);

                Assert.Equal(input.Length, decodedByteCount);
                Assert.Equal(input, decoded.Take(decodedByteCount));
            }
        }

        [Fact]
        public void HuffmanDecoding_InvalidEncoding_Throws()
        {
            foreach (byte[] encoded in InvalidEncodingData())
            {
                // Worst case decoding is an output byte per 5 input bits, so make the decoded buffer 2 times as big
                byte[] decoded = new byte[encoded.Length * 2];

                Assert.Throws<HuffmanDecodingException>(() => Huffman.Decode(encoded, ref decoded));
            }
        }

        // This input sequence will encode to 17 bits, thus offsetting the next character to encode
        // by exactly one bit. We use this below to generate a prefix that encodes all of the possible starting
        // bit offsets for a character, from 0 to 7.
        private static readonly byte[] s_offsetByOneBit = new byte[] { (byte)'c', (byte)'l', (byte)'r' };

        public static IEnumerable<byte[]> TestData()
        {
            // Single byte data
            for (int i = 0; i < 256; i++)
            {
                yield return new byte[] { (byte)i };
            }

            // Ensure that decoding every possible value leaves the decoder in a correct state so that
            // a subsequent value can be decoded (here, 'a')
            for (int i = 0; i < 256; i++)
            {
                yield return new byte[] { (byte)i, (byte)'a' };
            }

            // Ensure that every possible bit starting position for every value is encoded properly
            // s_offsetByOneBit encodes to exactly 17 bits, leaving 1 bit for the next byte
            // So by repeating this sequence, we can generate any starting bit position we want.
            byte[] currentPrefix = new byte[0];
            for (int prefixBits = 1; prefixBits <= 8; prefixBits++)
            {
                currentPrefix = currentPrefix.Concat(s_offsetByOneBit).ToArray();

                // Make sure we're actually getting the correct number of prefix bits
                int encodedBits = currentPrefix.Select(b => Huffman.Encode(b).bitLength).Sum();
                Assert.Equal(prefixBits % 8, encodedBits % 8);

                for (int i = 0; i < 256; i++)
                {
                    yield return currentPrefix.Concat(new byte[] { (byte)i }.Concat(currentPrefix)).ToArray();
                }
            }

            // Finally, one really big chunk of randomly generated data.
            byte[] data = new byte[1024 * 1024];
            new Random(42).NextBytes(data);
            yield return data;
        }

        private static IEnumerable<byte[]> InvalidEncodingData()
        {
            // For encodings greater than 8 bits, truncate one or more bytes to generate an invalid encoding
            byte[] source = new byte[1];
            byte[] destination = new byte[10];
            for (int i = 0; i < 256; i++)
            {
                source[0] = (byte)i;
                int encodedByteCount = Encode(source, destination, false);
                if (encodedByteCount > 1)
                {
                    yield return destination.Take(encodedByteCount - 1).ToArray();
                    if (encodedByteCount > 2)
                    {
                        yield return destination.Take(encodedByteCount - 2).ToArray();
                        if (encodedByteCount > 3)
                        {
                            yield return destination.Take(encodedByteCount - 3).ToArray();
                        }
                    }
                }
            }

            // Pad encodings with invalid trailing one bits. This is disallowed.
            byte[] pad1 = new byte[] { 0xFF };
            byte[] pad2 = new byte[] { 0xFF, 0xFF, };
            byte[] pad3 = new byte[] { 0xFF, 0xFF, 0xFF };
            byte[] pad4 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

            for (int i = 0; i < 256; i++)
            {
                source[0] = (byte)i;
                int encodedByteCount = Encode(source, destination, false);
                yield return destination.Take(encodedByteCount).Concat(pad1).ToArray();
                yield return destination.Take(encodedByteCount).Concat(pad2).ToArray();
                yield return destination.Take(encodedByteCount).Concat(pad3).ToArray();
                yield return destination.Take(encodedByteCount).Concat(pad4).ToArray();
            }

            // send single EOS
            yield return new byte[] { 0b11111111, 0b11111111, 0b11111111, 0b11111100 };

            // send combinations with EOS in the middle
            source = new byte[2];
            destination = new byte[24];
            for (int i = 0; i < 256; i++)
            {
                source[0] = source[1] = (byte)i;
                int encodedByteCount = Encode(source, destination, true);
                yield return destination.Take(encodedByteCount).ToArray();
            }
        }

        public static readonly TheoryData<byte[], byte[]> _validData = new TheoryData<byte[], byte[]>
        {
            // Single 5-bit symbol
            { new byte[] { 0x07 }, Encoding.ASCII.GetBytes("0") },
            // Single 6-bit symbol
            { new byte[] { 0x57 }, Encoding.ASCII.GetBytes("%") },
            // Single 7-bit symbol
            { new byte[] { 0xb9 }, Encoding.ASCII.GetBytes(":") },
            // Single 8-bit symbol
            { new byte[] { 0xf8 }, Encoding.ASCII.GetBytes("&") },
            // Single 10-bit symbol
            { new byte[] { 0xfe, 0x3f }, Encoding.ASCII.GetBytes("!") },
            // Single 11-bit symbol
            { new byte[] { 0xff, 0x7f }, Encoding.ASCII.GetBytes("+") },
            // Single 12-bit symbol
            { new byte[] { 0xff, 0xaf }, Encoding.ASCII.GetBytes("#") },
            // Single 13-bit symbol
            { new byte[] { 0xff, 0xcf }, Encoding.ASCII.GetBytes("$") },
            // Single 14-bit symbol
            { new byte[] { 0xff, 0xf3 }, Encoding.ASCII.GetBytes("^") },
            // Single 15-bit symbol
            { new byte[] { 0xff, 0xf9 }, Encoding.ASCII.GetBytes("<") },
            // Single 19-bit symbol
            { new byte[] { 0xff, 0xfe, 0x1f }, Encoding.ASCII.GetBytes("\\") },
            // Single 20-bit symbol
            { new byte[] { 0xff, 0xfe, 0x6f }, new byte[] { 0x80 } },
            // Single 21-bit symbol
            { new byte[] { 0xff, 0xfe, 0xe7 }, new byte[] { 0x99 } },
            // Single 22-bit symbol
            { new byte[] { 0xff, 0xff, 0x4b }, new byte[] { 0x81 } },
            // Single 23-bit symbol
            { new byte[] { 0xff, 0xff, 0xb1 }, new byte[] { 0x01 } },
            // Single 24-bit symbol
            { new byte[] { 0xff, 0xff, 0xea }, new byte[] { 0x09 } },
            // Single 25-bit symbol
            { new byte[] { 0xff, 0xff, 0xf6, 0x7f }, new byte[] { 0xc7 } },
            // Single 26-bit symbol
            { new byte[] { 0xff, 0xff, 0xf8, 0x3f }, new byte[] { 0xc0 } },
            // Single 27-bit symbol
            { new byte[] { 0xff, 0xff, 0xfb, 0xdf }, new byte[] { 0xcb } },
            // Single 28-bit symbol
            { new byte[] { 0xff, 0xff, 0xfe, 0x2f }, new byte[] { 0x02 } },
            // Single 30-bit symbol
            { new byte[] { 0xff, 0xff, 0xff, 0xf3 }, new byte[] { 0x0a } },

            //               h      e         l          l      o         *
            { new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1111 }, Encoding.ASCII.GetBytes("hello") },

            // Sequences that uncovered errors
            { new byte[] { 0xb6, 0xb9, 0xac, 0x1c, 0x85, 0x58, 0xd5, 0x20, 0xa4, 0xb6, 0xc2, 0xad, 0x61, 0x7b, 0x5a, 0x54, 0x25, 0x1f }, Encoding.ASCII.GetBytes("upgrade-insecure-requests") },
            { new byte[] { 0xfe, 0x53 }, Encoding.ASCII.GetBytes("\"t") }
        };

        [Theory]
        [MemberData(nameof(_validData))]
        public void HuffmanDecodeArray(byte[] encoded, byte[] expected)
        {
            byte[] dst = new byte[expected.Length];
            Assert.Equal(expected.Length, Huffman.Decode(new ReadOnlySpan<byte>(encoded), ref dst));
            Assert.Equal(expected, dst);
        }

        public static readonly TheoryData<byte[]> _longPaddingData = new TheoryData<byte[]>
        {
            //             h      e         l          l      o         *
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1111, 0b11111111 },

            // '&' (8 bits) + 8 bit padding
            new byte[] { 0xf8, 0xff },

            // ':' (7 bits) + 9 bit padding
            new byte[] { 0xb9, 0xff }
        };

        [Theory]
        [MemberData(nameof(_longPaddingData))]
        public void ThrowsOnPaddingLongerThanSevenBits(byte[] encoded)
        {
            byte[] dst = new byte[encoded.Length * 2];
            Exception exception = Assert.Throws<HuffmanDecodingException>(() => Huffman.Decode(new ReadOnlySpan<byte>(encoded), ref dst));
            Assert.Equal(SR.net_http_hpack_huffman_decode_failed, exception.Message);
        }

        public static readonly TheoryData<byte[]> _eosData = new TheoryData<byte[]>
        {
            // EOS
            new byte[] { 0xff, 0xff, 0xff, 0xff },
            // '&' + EOS + '0'
            new byte[] { 0xf8, 0xff, 0xff, 0xff, 0xfc, 0x1f }
        };

        [Theory]
        [MemberData(nameof(_eosData))]
        public void ThrowsOnEOS(byte[] encoded)
        {
            byte[] dst = new byte[encoded.Length * 2];
            Exception exception = Assert.Throws<HuffmanDecodingException>(() => Huffman.Decode(new ReadOnlySpan<byte>(encoded), ref dst));
            Assert.Equal(SR.net_http_hpack_huffman_decode_failed, exception.Message);
        }

        [Fact]
        public void ResizesOnDestinationBufferTooSmall()
        {
            //                           h      e         l          l      o         *
            byte[] encoded = new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1111 };
            byte[] originalDestination = new byte[encoded.Length];
            byte[] actualDestination = originalDestination;
            int decodedCount = Huffman.Decode(new ReadOnlySpan<byte>(encoded), ref actualDestination);
            Assert.Equal(5, decodedCount);
            Assert.NotSame(originalDestination, actualDestination);
        }

        public static readonly TheoryData<byte[]> _incompleteSymbolData = new TheoryData<byte[]>
        {
            //             h      e         l          l      o (incomplete)
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0 },

            // Non-zero padding will be seen as incomplete symbol
            //             h      e         l          l      o         *
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0000 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0001 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0010 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0011 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0100 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0101 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0110 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_0111 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1000 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1001 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1010 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1011 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1100 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1101 },
            new byte[] { 0b100111_00, 0b101_10100, 0b0_101000_0, 0b0111_1110 }
        };

        [Theory]
        [MemberData(nameof(_incompleteSymbolData))]
        public void ThrowsOnIncompleteSymbol(byte[] encoded)
        {
            byte[] dst = new byte[encoded.Length * 2];
            Exception exception = Assert.Throws<HuffmanDecodingException>(() => Huffman.Decode(new ReadOnlySpan<byte>(encoded), ref dst));
            Assert.Equal(SR.net_http_hpack_huffman_decode_failed, exception.Message);
        }

        [Fact]
        public void DecodeCharactersThatSpans5Octets()
        {
            int expectedLength = 2;
            byte[] decodedBytes = new byte[expectedLength];
            //                           B       LF                                             EOS
            byte[] encoded = new byte[] { 0b1011101_1, 0b11111111, 0b11111111, 0b11111111, 0b11100_111 };
            int decodedLength = Huffman.Decode(new ReadOnlySpan<byte>(encoded, 0, encoded.Length), ref decodedBytes);

            Assert.Equal(expectedLength, decodedLength);
            Assert.Equal(new byte[] { (byte)'B', (byte)'\n' }, decodedBytes);
        }

        [Theory]
        [MemberData(nameof(HuffmanData))]
        public void HuffmanEncode(int code, uint expectedEncoded, int expectedBitLength)
        {
            (uint encoded, int bitLength) = Huffman.Encode(code);
            Assert.Equal(expectedEncoded, encoded);
            Assert.Equal(expectedBitLength, bitLength);
        }

        [Theory]
        [MemberData(nameof(HuffmanData))]
        public void HuffmanDecode(int code, uint encoded, int bitLength)
        {
            Assert.Equal(code, Huffman.DecodeValue(encoded, bitLength, out int decodedBits));
            Assert.Equal(bitLength, decodedBits);
        }

        [Theory]
        [MemberData(nameof(HuffmanData))]
        public void HuffmanEncodeDecode(
            int code,
            // Suppresses the warning about an unused theory parameter because
            // this test shares data with other methods
#pragma warning disable xUnit1026
            uint encoded,
#pragma warning restore xUnit1026
            int bitLength)
        {
            Assert.Equal(code, Huffman.DecodeValue(Huffman.Encode(code).encoded, bitLength, out int decodedBits));
            Assert.Equal(bitLength, decodedBits);
        }

        public static TheoryData<int, uint, int> HuffmanData
        {
            get
            {
                TheoryData<int, uint, int> data = new TheoryData<int, uint, int>();

                data.Add(0, 0b11111111_11000000_00000000_00000000, 13);
                data.Add(1, 0b11111111_11111111_10110000_00000000, 23);
                data.Add(2, 0b11111111_11111111_11111110_00100000, 28);
                data.Add(3, 0b11111111_11111111_11111110_00110000, 28);
                data.Add(4, 0b11111111_11111111_11111110_01000000, 28);
                data.Add(5, 0b11111111_11111111_11111110_01010000, 28);
                data.Add(6, 0b11111111_11111111_11111110_01100000, 28);
                data.Add(7, 0b11111111_11111111_11111110_01110000, 28);
                data.Add(8, 0b11111111_11111111_11111110_10000000, 28);
                data.Add(9, 0b11111111_11111111_11101010_00000000, 24);
                data.Add(10, 0b11111111_11111111_11111111_11110000, 30);
                data.Add(11, 0b11111111_11111111_11111110_10010000, 28);
                data.Add(12, 0b11111111_11111111_11111110_10100000, 28);
                data.Add(13, 0b11111111_11111111_11111111_11110100, 30);
                data.Add(14, 0b11111111_11111111_11111110_10110000, 28);
                data.Add(15, 0b11111111_11111111_11111110_11000000, 28);
                data.Add(16, 0b11111111_11111111_11111110_11010000, 28);
                data.Add(17, 0b11111111_11111111_11111110_11100000, 28);
                data.Add(18, 0b11111111_11111111_11111110_11110000, 28);
                data.Add(19, 0b11111111_11111111_11111111_00000000, 28);
                data.Add(20, 0b11111111_11111111_11111111_00010000, 28);
                data.Add(21, 0b11111111_11111111_11111111_00100000, 28);
                data.Add(22, 0b11111111_11111111_11111111_11111000, 30);
                data.Add(23, 0b11111111_11111111_11111111_00110000, 28);
                data.Add(24, 0b11111111_11111111_11111111_01000000, 28);
                data.Add(25, 0b11111111_11111111_11111111_01010000, 28);
                data.Add(26, 0b11111111_11111111_11111111_01100000, 28);
                data.Add(27, 0b11111111_11111111_11111111_01110000, 28);
                data.Add(28, 0b11111111_11111111_11111111_10000000, 28);
                data.Add(29, 0b11111111_11111111_11111111_10010000, 28);
                data.Add(30, 0b11111111_11111111_11111111_10100000, 28);
                data.Add(31, 0b11111111_11111111_11111111_10110000, 28);
                data.Add(32, 0b01010000_00000000_00000000_00000000, 6);
                data.Add(33, 0b11111110_00000000_00000000_00000000, 10);
                data.Add(34, 0b11111110_01000000_00000000_00000000, 10);
                data.Add(35, 0b11111111_10100000_00000000_00000000, 12);
                data.Add(36, 0b11111111_11001000_00000000_00000000, 13);
                data.Add(37, 0b01010100_00000000_00000000_00000000, 6);
                data.Add(38, 0b11111000_00000000_00000000_00000000, 8);
                data.Add(39, 0b11111111_01000000_00000000_00000000, 11);
                data.Add(40, 0b11111110_10000000_00000000_00000000, 10);
                data.Add(41, 0b11111110_11000000_00000000_00000000, 10);
                data.Add(42, 0b11111001_00000000_00000000_00000000, 8);
                data.Add(43, 0b11111111_01100000_00000000_00000000, 11);
                data.Add(44, 0b11111010_00000000_00000000_00000000, 8);
                data.Add(45, 0b01011000_00000000_00000000_00000000, 6);
                data.Add(46, 0b01011100_00000000_00000000_00000000, 6);
                data.Add(47, 0b01100000_00000000_00000000_00000000, 6);
                data.Add(48, 0b00000000_00000000_00000000_00000000, 5);
                data.Add(49, 0b00001000_00000000_00000000_00000000, 5);
                data.Add(50, 0b00010000_00000000_00000000_00000000, 5);
                data.Add(51, 0b01100100_00000000_00000000_00000000, 6);
                data.Add(52, 0b01101000_00000000_00000000_00000000, 6);
                data.Add(53, 0b01101100_00000000_00000000_00000000, 6);
                data.Add(54, 0b01110000_00000000_00000000_00000000, 6);
                data.Add(55, 0b01110100_00000000_00000000_00000000, 6);
                data.Add(56, 0b01111000_00000000_00000000_00000000, 6);
                data.Add(57, 0b01111100_00000000_00000000_00000000, 6);
                data.Add(58, 0b10111000_00000000_00000000_00000000, 7);
                data.Add(59, 0b11111011_00000000_00000000_00000000, 8);
                data.Add(60, 0b11111111_11111000_00000000_00000000, 15);
                data.Add(61, 0b10000000_00000000_00000000_00000000, 6);
                data.Add(62, 0b11111111_10110000_00000000_00000000, 12);
                data.Add(63, 0b11111111_00000000_00000000_00000000, 10);
                data.Add(64, 0b11111111_11010000_00000000_00000000, 13);
                data.Add(65, 0b10000100_00000000_00000000_00000000, 6);
                data.Add(66, 0b10111010_00000000_00000000_00000000, 7);
                data.Add(67, 0b10111100_00000000_00000000_00000000, 7);
                data.Add(68, 0b10111110_00000000_00000000_00000000, 7);
                data.Add(69, 0b11000000_00000000_00000000_00000000, 7);
                data.Add(70, 0b11000010_00000000_00000000_00000000, 7);
                data.Add(71, 0b11000100_00000000_00000000_00000000, 7);
                data.Add(72, 0b11000110_00000000_00000000_00000000, 7);
                data.Add(73, 0b11001000_00000000_00000000_00000000, 7);
                data.Add(74, 0b11001010_00000000_00000000_00000000, 7);
                data.Add(75, 0b11001100_00000000_00000000_00000000, 7);
                data.Add(76, 0b11001110_00000000_00000000_00000000, 7);
                data.Add(77, 0b11010000_00000000_00000000_00000000, 7);
                data.Add(78, 0b11010010_00000000_00000000_00000000, 7);
                data.Add(79, 0b11010100_00000000_00000000_00000000, 7);
                data.Add(80, 0b11010110_00000000_00000000_00000000, 7);
                data.Add(81, 0b11011000_00000000_00000000_00000000, 7);
                data.Add(82, 0b11011010_00000000_00000000_00000000, 7);
                data.Add(83, 0b11011100_00000000_00000000_00000000, 7);
                data.Add(84, 0b11011110_00000000_00000000_00000000, 7);
                data.Add(85, 0b11100000_00000000_00000000_00000000, 7);
                data.Add(86, 0b11100010_00000000_00000000_00000000, 7);
                data.Add(87, 0b11100100_00000000_00000000_00000000, 7);
                data.Add(88, 0b11111100_00000000_00000000_00000000, 8);
                data.Add(89, 0b11100110_00000000_00000000_00000000, 7);
                data.Add(90, 0b11111101_00000000_00000000_00000000, 8);
                data.Add(91, 0b11111111_11011000_00000000_00000000, 13);
                data.Add(92, 0b11111111_11111110_00000000_00000000, 19);
                data.Add(93, 0b11111111_11100000_00000000_00000000, 13);
                data.Add(94, 0b11111111_11110000_00000000_00000000, 14);
                data.Add(95, 0b10001000_00000000_00000000_00000000, 6);
                data.Add(96, 0b11111111_11111010_00000000_00000000, 15);
                data.Add(97, 0b00011000_00000000_00000000_00000000, 5);
                data.Add(98, 0b10001100_00000000_00000000_00000000, 6);
                data.Add(99, 0b00100000_00000000_00000000_00000000, 5);
                data.Add(100, 0b10010000_00000000_00000000_00000000, 6);
                data.Add(101, 0b00101000_00000000_00000000_00000000, 5);
                data.Add(102, 0b10010100_00000000_00000000_00000000, 6);
                data.Add(103, 0b10011000_00000000_00000000_00000000, 6);
                data.Add(104, 0b10011100_00000000_00000000_00000000, 6);
                data.Add(105, 0b00110000_00000000_00000000_00000000, 5);
                data.Add(106, 0b11101000_00000000_00000000_00000000, 7);
                data.Add(107, 0b11101010_00000000_00000000_00000000, 7);
                data.Add(108, 0b10100000_00000000_00000000_00000000, 6);
                data.Add(109, 0b10100100_00000000_00000000_00000000, 6);
                data.Add(110, 0b10101000_00000000_00000000_00000000, 6);
                data.Add(111, 0b00111000_00000000_00000000_00000000, 5);
                data.Add(112, 0b10101100_00000000_00000000_00000000, 6);
                data.Add(113, 0b11101100_00000000_00000000_00000000, 7);
                data.Add(114, 0b10110000_00000000_00000000_00000000, 6);
                data.Add(115, 0b01000000_00000000_00000000_00000000, 5);
                data.Add(116, 0b01001000_00000000_00000000_00000000, 5);
                data.Add(117, 0b10110100_00000000_00000000_00000000, 6);
                data.Add(118, 0b11101110_00000000_00000000_00000000, 7);
                data.Add(119, 0b11110000_00000000_00000000_00000000, 7);
                data.Add(120, 0b11110010_00000000_00000000_00000000, 7);
                data.Add(121, 0b11110100_00000000_00000000_00000000, 7);
                data.Add(122, 0b11110110_00000000_00000000_00000000, 7);
                data.Add(123, 0b11111111_11111100_00000000_00000000, 15);
                data.Add(124, 0b11111111_10000000_00000000_00000000, 11);
                data.Add(125, 0b11111111_11110100_00000000_00000000, 14);
                data.Add(126, 0b11111111_11101000_00000000_00000000, 13);
                data.Add(127, 0b11111111_11111111_11111111_11000000, 28);
                data.Add(128, 0b11111111_11111110_01100000_00000000, 20);
                data.Add(129, 0b11111111_11111111_01001000_00000000, 22);
                data.Add(130, 0b11111111_11111110_01110000_00000000, 20);
                data.Add(131, 0b11111111_11111110_10000000_00000000, 20);
                data.Add(132, 0b11111111_11111111_01001100_00000000, 22);
                data.Add(133, 0b11111111_11111111_01010000_00000000, 22);
                data.Add(134, 0b11111111_11111111_01010100_00000000, 22);
                data.Add(135, 0b11111111_11111111_10110010_00000000, 23);
                data.Add(136, 0b11111111_11111111_01011000_00000000, 22);
                data.Add(137, 0b11111111_11111111_10110100_00000000, 23);
                data.Add(138, 0b11111111_11111111_10110110_00000000, 23);
                data.Add(139, 0b11111111_11111111_10111000_00000000, 23);
                data.Add(140, 0b11111111_11111111_10111010_00000000, 23);
                data.Add(141, 0b11111111_11111111_10111100_00000000, 23);
                data.Add(142, 0b11111111_11111111_11101011_00000000, 24);
                data.Add(143, 0b11111111_11111111_10111110_00000000, 23);
                data.Add(144, 0b11111111_11111111_11101100_00000000, 24);
                data.Add(145, 0b11111111_11111111_11101101_00000000, 24);
                data.Add(146, 0b11111111_11111111_01011100_00000000, 22);
                data.Add(147, 0b11111111_11111111_11000000_00000000, 23);
                data.Add(148, 0b11111111_11111111_11101110_00000000, 24);
                data.Add(149, 0b11111111_11111111_11000010_00000000, 23);
                data.Add(150, 0b11111111_11111111_11000100_00000000, 23);
                data.Add(151, 0b11111111_11111111_11000110_00000000, 23);
                data.Add(152, 0b11111111_11111111_11001000_00000000, 23);
                data.Add(153, 0b11111111_11111110_11100000_00000000, 21);
                data.Add(154, 0b11111111_11111111_01100000_00000000, 22);
                data.Add(155, 0b11111111_11111111_11001010_00000000, 23);
                data.Add(156, 0b11111111_11111111_01100100_00000000, 22);
                data.Add(157, 0b11111111_11111111_11001100_00000000, 23);
                data.Add(158, 0b11111111_11111111_11001110_00000000, 23);
                data.Add(159, 0b11111111_11111111_11101111_00000000, 24);
                data.Add(160, 0b11111111_11111111_01101000_00000000, 22);
                data.Add(161, 0b11111111_11111110_11101000_00000000, 21);
                data.Add(162, 0b11111111_11111110_10010000_00000000, 20);
                data.Add(163, 0b11111111_11111111_01101100_00000000, 22);
                data.Add(164, 0b11111111_11111111_01110000_00000000, 22);
                data.Add(165, 0b11111111_11111111_11010000_00000000, 23);
                data.Add(166, 0b11111111_11111111_11010010_00000000, 23);
                data.Add(167, 0b11111111_11111110_11110000_00000000, 21);
                data.Add(168, 0b11111111_11111111_11010100_00000000, 23);
                data.Add(169, 0b11111111_11111111_01110100_00000000, 22);
                data.Add(170, 0b11111111_11111111_01111000_00000000, 22);
                data.Add(171, 0b11111111_11111111_11110000_00000000, 24);
                data.Add(172, 0b11111111_11111110_11111000_00000000, 21);
                data.Add(173, 0b11111111_11111111_01111100_00000000, 22);
                data.Add(174, 0b11111111_11111111_11010110_00000000, 23);
                data.Add(175, 0b11111111_11111111_11011000_00000000, 23);
                data.Add(176, 0b11111111_11111111_00000000_00000000, 21);
                data.Add(177, 0b11111111_11111111_00001000_00000000, 21);
                data.Add(178, 0b11111111_11111111_10000000_00000000, 22);
                data.Add(179, 0b11111111_11111111_00010000_00000000, 21);
                data.Add(180, 0b11111111_11111111_11011010_00000000, 23);
                data.Add(181, 0b11111111_11111111_10000100_00000000, 22);
                data.Add(182, 0b11111111_11111111_11011100_00000000, 23);
                data.Add(183, 0b11111111_11111111_11011110_00000000, 23);
                data.Add(184, 0b11111111_11111110_10100000_00000000, 20);
                data.Add(185, 0b11111111_11111111_10001000_00000000, 22);
                data.Add(186, 0b11111111_11111111_10001100_00000000, 22);
                data.Add(187, 0b11111111_11111111_10010000_00000000, 22);
                data.Add(188, 0b11111111_11111111_11100000_00000000, 23);
                data.Add(189, 0b11111111_11111111_10010100_00000000, 22);
                data.Add(190, 0b11111111_11111111_10011000_00000000, 22);
                data.Add(191, 0b11111111_11111111_11100010_00000000, 23);
                data.Add(192, 0b11111111_11111111_11111000_00000000, 26);
                data.Add(193, 0b11111111_11111111_11111000_01000000, 26);
                data.Add(194, 0b11111111_11111110_10110000_00000000, 20);
                data.Add(195, 0b11111111_11111110_00100000_00000000, 19);
                data.Add(196, 0b11111111_11111111_10011100_00000000, 22);
                data.Add(197, 0b11111111_11111111_11100100_00000000, 23);
                data.Add(198, 0b11111111_11111111_10100000_00000000, 22);
                data.Add(199, 0b11111111_11111111_11110110_00000000, 25);
                data.Add(200, 0b11111111_11111111_11111000_10000000, 26);
                data.Add(201, 0b11111111_11111111_11111000_11000000, 26);
                data.Add(202, 0b11111111_11111111_11111001_00000000, 26);
                data.Add(203, 0b11111111_11111111_11111011_11000000, 27);
                data.Add(204, 0b11111111_11111111_11111011_11100000, 27);
                data.Add(205, 0b11111111_11111111_11111001_01000000, 26);
                data.Add(206, 0b11111111_11111111_11110001_00000000, 24);
                data.Add(207, 0b11111111_11111111_11110110_10000000, 25);
                data.Add(208, 0b11111111_11111110_01000000_00000000, 19);
                data.Add(209, 0b11111111_11111111_00011000_00000000, 21);
                data.Add(210, 0b11111111_11111111_11111001_10000000, 26);
                data.Add(211, 0b11111111_11111111_11111100_00000000, 27);
                data.Add(212, 0b11111111_11111111_11111100_00100000, 27);
                data.Add(213, 0b11111111_11111111_11111001_11000000, 26);
                data.Add(214, 0b11111111_11111111_11111100_01000000, 27);
                data.Add(215, 0b11111111_11111111_11110010_00000000, 24);
                data.Add(216, 0b11111111_11111111_00100000_00000000, 21);
                data.Add(217, 0b11111111_11111111_00101000_00000000, 21);
                data.Add(218, 0b11111111_11111111_11111010_00000000, 26);
                data.Add(219, 0b11111111_11111111_11111010_01000000, 26);
                data.Add(220, 0b11111111_11111111_11111111_11010000, 28);
                data.Add(221, 0b11111111_11111111_11111100_01100000, 27);
                data.Add(222, 0b11111111_11111111_11111100_10000000, 27);
                data.Add(223, 0b11111111_11111111_11111100_10100000, 27);
                data.Add(224, 0b11111111_11111110_11000000_00000000, 20);
                data.Add(225, 0b11111111_11111111_11110011_00000000, 24);
                data.Add(226, 0b11111111_11111110_11010000_00000000, 20);
                data.Add(227, 0b11111111_11111111_00110000_00000000, 21);
                data.Add(228, 0b11111111_11111111_10100100_00000000, 22);
                data.Add(229, 0b11111111_11111111_00111000_00000000, 21);
                data.Add(230, 0b11111111_11111111_01000000_00000000, 21);
                data.Add(231, 0b11111111_11111111_11100110_00000000, 23);
                data.Add(232, 0b11111111_11111111_10101000_00000000, 22);
                data.Add(233, 0b11111111_11111111_10101100_00000000, 22);
                data.Add(234, 0b11111111_11111111_11110111_00000000, 25);
                data.Add(235, 0b11111111_11111111_11110111_10000000, 25);
                data.Add(236, 0b11111111_11111111_11110100_00000000, 24);
                data.Add(237, 0b11111111_11111111_11110101_00000000, 24);
                data.Add(238, 0b11111111_11111111_11111010_10000000, 26);
                data.Add(239, 0b11111111_11111111_11101000_00000000, 23);
                data.Add(240, 0b11111111_11111111_11111010_11000000, 26);
                data.Add(241, 0b11111111_11111111_11111100_11000000, 27);
                data.Add(242, 0b11111111_11111111_11111011_00000000, 26);
                data.Add(243, 0b11111111_11111111_11111011_01000000, 26);
                data.Add(244, 0b11111111_11111111_11111100_11100000, 27);
                data.Add(245, 0b11111111_11111111_11111101_00000000, 27);
                data.Add(246, 0b11111111_11111111_11111101_00100000, 27);
                data.Add(247, 0b11111111_11111111_11111101_01000000, 27);
                data.Add(248, 0b11111111_11111111_11111101_01100000, 27);
                data.Add(249, 0b11111111_11111111_11111111_11100000, 28);
                data.Add(250, 0b11111111_11111111_11111101_10000000, 27);
                data.Add(251, 0b11111111_11111111_11111101_10100000, 27);
                data.Add(252, 0b11111111_11111111_11111101_11000000, 27);
                data.Add(253, 0b11111111_11111111_11111101_11100000, 27);
                data.Add(254, 0b11111111_11111111_11111110_00000000, 27);
                data.Add(255, 0b11111111_11111111_11111011_10000000, 26);
                data.Add(256, 0b11111111_11111111_11111111_11111100, 30);

                return data;
            }
        }
    }
}
