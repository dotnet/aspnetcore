// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HPackIntegerTests
    {
        [Fact]
        public void IntegerEncoderDecoderRoundtrips()
        {
            var decoder = new IntegerDecoder();
            var range = 1 << 8;

            foreach (var i in Enumerable.Range(0, range).Concat(Enumerable.Range(int.MaxValue - range + 1, range)))
            {
                for (int n = 1; n <= 8; n++)
                {
                    var integerBytes = new byte[6];
                    Assert.True(IntegerEncoder.Encode(i, n, integerBytes, out var length));

                    var decodeResult = decoder.BeginTryDecode(integerBytes[0], n, out var intResult);

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

        [Theory]
        [MemberData(nameof(IntegerCodecSamples))]
        public void EncodeSamples(int value, int bits, byte[] expectedResult)
        {
            Span<byte> actualResult = new byte[64];
            bool success = IntegerEncoder.Encode(value, bits, actualResult, out int bytesWritten);

            Assert.True(success);
            Assert.Equal(expectedResult.Length, bytesWritten);
            Assert.True(actualResult.Slice(0, bytesWritten).SequenceEqual(expectedResult));
        }

        [Theory]
        [MemberData(nameof(IntegerCodecSamples))]
        public void EncodeSamplesWithShortBuffer(int value, int bits, byte[] expectedResult)
        {
            Span<byte> actualResult = new byte[expectedResult.Length - 1];
            bool success = IntegerEncoder.Encode(value, bits, actualResult, out int bytesWritten);

            Assert.False(success);
        }

        [Theory]
        [MemberData(nameof(IntegerCodecSamples))]
        public void DecodeSamples(int expectedResult, int bits, byte[] encoded)
        {
            var integerDecoder = new IntegerDecoder();

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

        // integer, prefix length, encoded
        public static IEnumerable<object[]> IntegerCodecSamples()
        {
            yield return new object[] { 10, 5, new byte[] { 0x0A } };
            yield return new object[] { 1337, 5, new byte[] { 0x1F, 0x9A, 0x0A } };
            yield return new object[] { 42, 8, new byte[] { 0x2A } };
        }
    }
}
