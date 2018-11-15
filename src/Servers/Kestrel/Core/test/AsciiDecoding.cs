// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Numerics;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class AsciiDecodingTests
    {
        [Fact]
        private void FullAsciiRangeSupported()
        {
            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x);

            var byteArray = byteRange
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .Concat(byteRange)
                .ToArray();

            var s = new Span<byte>(byteArray).GetAsciiStringNonNullCharacters();

            Assert.Equal(s.Length, byteArray.Length);

            for (var i = 1; i < byteArray.Length; i++)
            {
                var sb = (byte)s[i];
                var b = byteArray[i];

                Assert.Equal(sb, b);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x80)]
        private void ExceptionThrownForZeroOrNonAscii(byte b)
        {
            for (var length = 1; length < Vector<sbyte>.Count * 4; length++)
            {
                for (var position = 0; position < length; position++)
                {
                    var byteRange = Enumerable.Range(1, length).Select(x => (byte)x).ToArray();
                    byteRange[position] = b;
                    
                    Assert.Throws<InvalidOperationException>(() => new Span<byte>(byteRange).GetAsciiStringNonNullCharacters());
                }
            }
        }

        [Fact]
        private void LargeAllocationProducesCorrectResults()
        {
            var byteRange = Enumerable.Range(0, 16384 + 64).Select(x => (byte)((x & 0x7f) | 0x01)).ToArray();
            var expectedByteRange = byteRange.Concat(byteRange).ToArray();
            
            var s = new Span<byte>(expectedByteRange).GetAsciiStringNonNullCharacters();

            Assert.Equal(expectedByteRange.Length, s.Length);

            for (var i = 0; i < expectedByteRange.Length; i++)
            {
                var sb = (byte)((s[i] & 0x7f) | 0x01);
                var b = expectedByteRange[i];

                Assert.Equal(sb, b);
            }
        }
    }
}
