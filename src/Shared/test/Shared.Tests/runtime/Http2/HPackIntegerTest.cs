// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http.HPack;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class HPackIntegerTest
    {
        [Theory]
        [MemberData(nameof(IntegerCodecExactSamples))]
        public void HPack_IntegerEncode(int value, int bits, byte[] expectedResult)
        {
            Span<byte> actualResult = new byte[64];
            bool success = IntegerEncoder.Encode(value, bits, actualResult, out int bytesWritten);

            Assert.True(success);
            Assert.Equal(expectedResult.Length, bytesWritten);
            Assert.True(actualResult.Slice(0, bytesWritten).SequenceEqual(expectedResult));
        }

        [Theory]
        [MemberData(nameof(IntegerCodecExactSamples))]
        public void HPack_IntegerEncode_ShortBuffer(int value, int bits, byte[] expectedResult)
        {
            Span<byte> actualResult = new byte[expectedResult.Length - 1];
            bool success = IntegerEncoder.Encode(value, bits, actualResult, out int bytesWritten);

            Assert.False(success);
        }

        [Theory]
        [MemberData(nameof(IntegerCodecExactSamples))]
        public void HPack_IntegerDecode(int expectedResult, int bits, byte[] encoded)
        {
            IntegerDecoder integerDecoder = new IntegerDecoder();

            bool finished = integerDecoder.BeginTryDecode(encoded[0], bits, out int actualResult);

            int i = 1;
            for (; !finished && i < encoded.Length; ++i)
            {
                finished = integerDecoder.TryDecode(encoded[i], out actualResult);
            }

            Assert.True(finished);
            Assert.Equal(encoded.Length, i);

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void IntegerEncoderDecoderRoundtrips()
        {
            IntegerDecoder decoder = new IntegerDecoder();

            for (int i = 0; i < 2048; ++i)
            {
                for (int prefixLength = 1; prefixLength <= 8; ++prefixLength)
                {
                    Span<byte> integerBytes = stackalloc byte[5];
                    Assert.True(IntegerEncoder.Encode(i, prefixLength, integerBytes, out int length));

                    bool decodeResult = decoder.BeginTryDecode(integerBytes[0], prefixLength, out int intResult);

                    for (int j = 1; j < length; j++)
                    {
                        Assert.False(decodeResult);
                        decodeResult = decoder.TryDecode(integerBytes[j], out intResult);
                    }

                    Assert.True(decodeResult);
                    Assert.Equal(i, intResult);
                }
            }
        }

        public static IEnumerable<object[]> IntegerCodecExactSamples()
        {
            yield return new object[] { 10, 5, new byte[] { 0x0A } };
            yield return new object[] { 1337, 5, new byte[] { 0x1F, 0x9A, 0x0A } };
            yield return new object[] { 42, 8, new byte[] { 0x2A } };
            yield return new object[] { 7, 3, new byte[] { 0x7, 0x0 } };
            yield return new object[] { int.MaxValue, 1, new byte[] { 0x01, 0xfe, 0xff, 0xff, 0xff, 0x07 } };
            yield return new object[] { int.MaxValue, 8, new byte[] { 0xff, 0x80, 0xfe, 0xff, 0xff, 0x07 } };
        }

        [Theory]
        [MemberData(nameof(IntegerData_OverMax))]
        public void IntegerDecode_Throws_IfMaxExceeded(int prefixLength, byte[] octets)
        {
            var decoder = new IntegerDecoder();
            var result = decoder.BeginTryDecode(octets[0], prefixLength, out var intResult);

            for (var j = 1; j < octets.Length - 1; j++)
            {
                Assert.False(decoder.TryDecode(octets[j], out intResult));
            }

            Assert.Throws<HPackDecodingException>(() => decoder.TryDecode(octets[octets.Length - 1], out intResult));
        }

        public static TheoryData<int, byte[]> IntegerData_OverMax
        {
            get
            {
                var data = new TheoryData<int, byte[]>();

                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x07 }); // Int32.MaxValue + 1
                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x08 }); // MSB exceeds maximum
                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x80 }); // Undefined since continuation bit set

                data.Add(7, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0x08 }); // 1 bit too large
                data.Add(7, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F });
                data.Add(7, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0x80 }); // A continuation byte (0x80) where the byte after it would be too large.
                data.Add(7, new byte[] { 0x7F, 0xFF, 0x00 }); // Encoded with 1 byte too many.

                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x07 }); // Int32.MaxValue + 1
                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x08 }); // MSB exceeds maximum
                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x80 }); // Undefined since continuation bit set

                return data;
            }
        }
    }
}
