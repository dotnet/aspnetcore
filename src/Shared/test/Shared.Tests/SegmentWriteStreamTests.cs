// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WriteStream.Tests;

public class SegmentWriteStreamTests
{
    private static readonly byte[] WriteData = new byte[]
    {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SegmentWriteStream_InvalidSegmentSize_Throws(int segmentSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SegmentWriteStream(segmentSize));
    }

    [Fact]
    public void ReadAndSeekOperations_Throws()
    {
        var stream = new SegmentWriteStream(1);

        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 0));
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void GetSegments_ExtractionDisablesWriting()
    {
        var stream = new SegmentWriteStream(1);

        Assert.True(stream.CanWrite);
        Assert.Empty(stream.GetSegments());
        Assert.False(stream.CanWrite);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void WriteByte_CanWriteAllBytes(int segmentSize)
    {
        var stream = new SegmentWriteStream(segmentSize);

        foreach (var datum in WriteData)
        {
            stream.WriteByte(datum);
        }
        var segments = stream.GetSegments();

        Assert.Equal(WriteData.Length, stream.Length);
        Assert.Equal((WriteData.Length + segmentSize - 1) / segmentSize, segments.Count);

        for (var i = 0; i < WriteData.Length; i += segmentSize)
        {
            var expectedSegmentSize = Math.Min(segmentSize, WriteData.Length - i);
            var expectedSegment = new byte[expectedSegmentSize];
            for (int j = 0; j < expectedSegmentSize; j++)
            {
                expectedSegment[j] = (byte)(i + j);
            }
            var segment = segments[i / segmentSize];

            Assert.Equal(expectedSegmentSize, segment.Length);
            Assert.True(expectedSegment.SequenceEqual(segment));
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Write_CanWriteAllBytes(int writeSize)
    {
        var segmentSize = 5;
        var stream = new SegmentWriteStream(segmentSize);

        for (var i = 0; i < WriteData.Length; i += writeSize)
        {
            stream.Write(WriteData, i, Math.Min(writeSize, WriteData.Length - i));
        }
        var segments = stream.GetSegments();

        Assert.Equal(WriteData.Length, stream.Length);
        Assert.Equal((WriteData.Length + segmentSize - 1) / segmentSize, segments.Count);

        for (var i = 0; i < WriteData.Length; i += segmentSize)
        {
            var expectedSegmentSize = Math.Min(segmentSize, WriteData.Length - i);
            var expectedSegment = new byte[expectedSegmentSize];
            for (int j = 0; j < expectedSegmentSize; j++)
            {
                expectedSegment[j] = (byte)(i + j);
            }
            var segment = segments[i / segmentSize];

            Assert.Equal(expectedSegmentSize, segment.Length);
            Assert.True(expectedSegment.SequenceEqual(segment));
        }
    }
}
