// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

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

    [Theory]
    [InlineData(20, false)]
    [InlineData(21, true)]
    [InlineData(22, false)]
    [InlineData(261, false)]
    [InlineData(262, true)]
    [InlineData(263, false)]
    [InlineData(4102, false)]
    [InlineData(4103, true)]
    [InlineData(4104, false)]
    [InlineData(65543, false)]
    [InlineData(65544, true)]
    [InlineData(65545, false)]
    [InlineData(1048584, false)]
    [InlineData(1048585, true)]
    [InlineData(1048586, false)]
    [InlineData(16777225, false)]
    [InlineData(16777226, true)]
    [InlineData(16777227, false)]
    [InlineData(268435466, false)]
    [InlineData(268435467, true)]
    [InlineData(268435468, false)]
    public void ChunkedPrefixReturnsSegmentThatDoesNotNeedToMove(int dataCount, bool expectSlice)
    {
        // Will call GetMemory on at least 5 bytes from the Http1OutputProducer
        var prefixLength = ChunkWriter.GetPrefixBytesForChunk(dataCount, out var actualSliceOneByte);
        if (actualSliceOneByte)
        {
            dataCount--;
        }

        var fakeMemory = new Memory<byte>(new byte[16]);

        var actualLength = ChunkWriter.BeginChunkBytes(dataCount - prefixLength - 2, fakeMemory.Span);

        Assert.Equal(prefixLength, actualLength);
        Assert.Equal(expectSlice, actualSliceOneByte);
    }
}
