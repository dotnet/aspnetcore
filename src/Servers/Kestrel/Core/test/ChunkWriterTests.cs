// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ChunkWriterTests
    {
        [Theory]
        [InlineData(0x00, "0\r\n")]
        [InlineData(0x01, "1\r\n")]
        [InlineData(0x08, "8\r\n")]
        [InlineData(0x0a, "a\r\n")]
        [InlineData(0x0f, "f\r\n")]
        [InlineData(0x010, "10\r\n")]
        [InlineData(0x080, "80\r\n")]
        [InlineData(0x0ff, "ff\r\n")]
        [InlineData(0x0100, "100\r\n")]
        [InlineData(0x0800, "800\r\n")]
        [InlineData(0x0fff, "fff\r\n")]
        [InlineData(0x01000, "1000\r\n")]
        [InlineData(0x08000, "8000\r\n")]
        [InlineData(0x0ffff, "ffff\r\n")]
        [InlineData(0x010000, "10000\r\n")]
        [InlineData(0x080000, "80000\r\n")]
        [InlineData(0x0fffff, "fffff\r\n")]
        [InlineData(0x0100000, "100000\r\n")]
        [InlineData(0x0800000, "800000\r\n")]
        [InlineData(0x0ffffff, "ffffff\r\n")]
        [InlineData(0x01000000, "1000000\r\n")]
        [InlineData(0x08000000, "8000000\r\n")]
        [InlineData(0x0fffffff, "fffffff\r\n")]
        [InlineData(0x010000000, "10000000\r\n")]
        [InlineData(0x7fffffffL, "7fffffff\r\n")]
        public void ChunkedPrefixMustBeHexCrLfWithoutLeadingZeros(int dataCount, string expected)
        {
            Span<byte> span = new byte[10];
            var count = ChunkWriter.BeginChunkBytes(dataCount, span);

            Assert.Equal(Encoding.ASCII.GetBytes(expected), span.Slice(0, count).ToArray());
        }

        [Fact]
        public void ChunkedPrefixReturnsSegmentThatDoesNotNeedToMove()
        {
            var memory = new Memory<byte>(new byte[10000000]);
            // Will call GetMemory on at least 5 bytes from the Http1OutputProducer
            for (var i = 5; i < 10000000; i++)
            {
                var currentMemory = memory.Slice(0, i);
                var prefixLength = ChunkWriter.GetPrefixBytesForChunk(ref currentMemory);
                var slicedMemory = currentMemory.Slice(prefixLength, currentMemory.Length - prefixLength - 2);
                var actualLength = ChunkWriter.BeginChunkBytes(slicedMemory.Length, currentMemory.Span);

                Assert.Equal(prefixLength, actualLength);
            }
        }
    }
}
