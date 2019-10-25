// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HPackIntegerTests
    {

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
