// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Pipelines.Tests;

public class BufferWriterTests : IDisposable
{
    protected Pipe Pipe;
    public BufferWriterTests()
    {
        Pipe = new Pipe(new PipeOptions(useSynchronizationContext: false, pauseWriterThreshold: 0, resumeWriterThreshold: 0));
    }

    public void Dispose()
    {
        Pipe.Writer.Complete();
        Pipe.Reader.Complete();
    }

    private byte[] Read()
    {
        Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
        Pipe.Writer.Complete();
        ReadResult readResult = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        byte[] data = readResult.Buffer.ToArray();
        Pipe.Reader.AdvanceTo(readResult.Buffer.End);
        return data;
    }

    [Theory]
    [InlineData(3, -1, 0)]
    [InlineData(3, 0, -1)]
    [InlineData(3, 0, 4)]
    [InlineData(3, 4, 0)]
    [InlineData(3, -1, -1)]
    [InlineData(3, 4, 4)]
    public void ThrowsForInvalidParameters(int arrayLength, int offset, int length)
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        var array = new byte[arrayLength];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = (byte)(i + 1);
        }

        writer.Write(new Span<byte>(array, 0, 0));
        writer.Write(new Span<byte>(array, array.Length, 0));

        try
        {
            writer.Write(new Span<byte>(array, offset, length));
            Assert.True(false);
        }
        catch (Exception ex)
        {
            Assert.True(ex is ArgumentOutOfRangeException);
        }

        writer.Write(new Span<byte>(array, 0, array.Length));
        writer.Commit();

        Assert.Equal(array, Read());
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(1, 1)]
    public void CanWriteWithOffsetAndLength(int offset, int length)
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        var array = new byte[] { 1, 2, 3 };

        writer.Write(new Span<byte>(array, offset, length));

        Assert.Equal(0, writer.BytesCommitted);

        writer.Commit();

        Assert.Equal(length, writer.BytesCommitted);
        Assert.Equal(array.Skip(offset).Take(length).ToArray(), Read());
        Assert.Equal(length, writer.BytesCommitted);
    }

    [Fact]
    public void CanWriteEmpty()
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        var array = new byte[] { };

        writer.Write(array);
        writer.Write(new Span<byte>(array, 0, array.Length));
        writer.Commit();

        Assert.Equal(0, writer.BytesCommitted);
        Assert.Equal(array, Read());
    }

    [Fact]
    public void CanWriteIntoHeadlessBuffer()
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);

        writer.Write(new byte[] { 1, 2, 3 });
        writer.Commit();

        Assert.Equal(3, writer.BytesCommitted);
        Assert.Equal(new byte[] { 1, 2, 3 }, Read());
    }

    [Fact]
    public void CanWriteMultipleTimes()
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);

        writer.Write(new byte[] { 1 });
        writer.Write(new byte[] { 2 });
        writer.Write(new byte[] { 3 });
        writer.Commit();

        Assert.Equal(3, writer.BytesCommitted);
        Assert.Equal(new byte[] { 1, 2, 3 }, Read());
    }

    [Fact]
    public void CanWriteOverTheBlockLength()
    {
        Memory<byte> memory = Pipe.Writer.GetMemory();
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);

        IEnumerable<byte> source = Enumerable.Range(0, memory.Length).Select(i => (byte)i);
        byte[] expectedBytes = source.Concat(source).Concat(source).ToArray();

        writer.Write(expectedBytes);
        writer.Commit();

        Assert.Equal(expectedBytes.LongLength, writer.BytesCommitted);
        Assert.Equal(expectedBytes, Read());
    }

    [Fact]
    public void EnsureAllocatesSpan()
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        writer.Ensure(10);
        Assert.True(writer.Span.Length > 10);
        Assert.Equal(0, writer.BytesCommitted);
        Assert.Equal(new byte[] { }, Read());
    }

    [Fact]
    public void ExposesSpan()
    {
        int initialLength = Pipe.Writer.GetMemory().Length;
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        Assert.Equal(initialLength, writer.Span.Length);
        Assert.Equal(new byte[] { }, Read());
    }

    [Fact]
    public void SlicesSpanAndAdvancesAfterWrite()
    {
        int initialLength = Pipe.Writer.GetMemory().Length;

        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);

        writer.Write(new byte[] { 1, 2, 3 });
        writer.Commit();

        Assert.Equal(3, writer.BytesCommitted);
        Assert.Equal(initialLength - 3, writer.Span.Length);
        Assert.Equal(Pipe.Writer.GetMemory().Length, writer.Span.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, Read());
    }

    [Fact]
    public void BufferWriterCountsBytesCommitted()
    {
        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);

        writer.Write(new byte[] { 1, 2, 3 });
        Assert.Equal(0, writer.BytesCommitted);

        writer.Commit();
        Assert.Equal(3, writer.BytesCommitted);

        writer.Ensure(10);
        writer.Advance(10);
        Assert.Equal(3, writer.BytesCommitted);

        writer.Commit();
        Assert.Equal(13, writer.BytesCommitted);

        Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
        var readResult = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();

        // Consuming the buffer does not change BytesCommitted
        Assert.Equal(13, readResult.Buffer.Length);
        Assert.Equal(13, writer.BytesCommitted);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(500)]
    [InlineData(5000)]
    [InlineData(50000)]
    public void WriteLargeDataBinary(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);

        BufferWriter<PipeWriter> writer = new BufferWriter<PipeWriter>(Pipe.Writer);
        writer.Write(data);
        writer.Commit();

        Assert.Equal(length, writer.BytesCommitted);
        Assert.Equal(data, Read());
    }
}
