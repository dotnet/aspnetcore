// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class AsciiDecodingTests
    {
        [Fact]
        private void FullAsciiRangeSupported()
        {
            var byteRange = Enumerable.Range(1, 127).Select(x => (byte)x).ToArray();
            var s = new Span<byte>(byteRange).GetAsciiStringNonNullCharacters();

            Assert.Equal(s.Length, byteRange.Length);

            for (var i = 1; i < byteRange.Length; i++)
            {
                var sb = (byte)s[i];
                var b = byteRange[i];

                Assert.Equal(sb, b);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x80)]
        private void ExceptionThrownForZeroOrNonAscii(byte b)
        {
            for (var length = 1; length < 16; length++)
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
