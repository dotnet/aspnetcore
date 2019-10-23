// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.Http.HPack;

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
        [MemberData(nameof(IntegerCodecRoundTripSamples))]
        public void HPack_IntegerRoundTrip(int value, int bits)
        {
            var decoder = new IntegerDecoder();

            Span<byte> encoded = stackalloc byte[5];
            Assert.True(IntegerEncoder.Encode(value, bits, encoded, out int bytesWritten));

            bool finished = decoder.BeginTryDecode(encoded[0], bits, out int intResult);

            int i = 1;
            for (; !finished && i < encoded.Length; ++i)
            {
                finished = decoder.TryDecode(encoded[i], out intResult);
            }

            Assert.True(finished);
            Assert.Equal(bytesWritten, i);
            Assert.Equal(value, intResult);
        }

        public static IEnumerable<object[]> IntegerCodecExactSamples()
        {
            yield return new object[] { 10, 5, new byte[] { 0x0A } };
            yield return new object[] { 1337, 5, new byte[] { 0x1F, 0x9A, 0x0A } };
            yield return new object[] { 42, 8, new byte[] { 0x2A } };
        }

        public static IEnumerable<object[]> IntegerCodecRoundTripSamples()
        {
            for (int i = 0; i < 2048; ++i)
            {
                for (int prefixLength = 1; prefixLength <= 8; ++prefixLength)
                {
                    yield return new object[] { i, prefixLength };
                }
            }
        }

        [Theory]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x08 })] // 1 bit too large
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F })]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x80 })] // A continuation byte (0x80) where the byte after it would be too large.
        [InlineData(new byte[] { 0xFF, 0xFF, 0x00 })] // Encoded with 1 byte too many.
        public void HPack_Integer_TooLarge(byte[] encoded)
        {
            Assert.Throws<HPackDecodingException>(() =>
            {
                var dec = new IntegerDecoder();

                if (!dec.BeginTryDecode((byte)(encoded[0] & 0x7F), 7, out int intResult))
                {
                    for (int i = 1; !dec.TryDecode(encoded[i], out intResult); ++i)
                    {
                    }
                }

                return intResult;
            });
        }
    }
}
