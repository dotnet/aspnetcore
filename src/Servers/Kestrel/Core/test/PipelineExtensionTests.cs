// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class PipelineExtensionTests : IDisposable
{
    // ulong.MaxValue.ToString().Length
    private const int _ulongMaxValueLength = 20;

    private readonly Pipe _pipe;
    private readonly MemoryPool<byte> _memoryPool = PinnedBlockMemoryPoolFactory.Create();

    public PipelineExtensionTests()
    {
        _pipe = new Pipe(new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
    }

    public void Dispose()
    {
        _pipe.Reader.Complete();
        _pipe.Writer.Complete();
        _memoryPool.Dispose();
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(4_8_15_16_23_42)]
    public void WritesNumericToAscii(ulong number)
    {
        var writerBuffer = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(writerBuffer);
        writer.WriteNumeric(number);
        writer.Commit();
        writerBuffer.FlushAsync().GetAwaiter().GetResult();

        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        var numAsStr = number.ToString(CultureInfo.InvariantCulture);
        var expected = Encoding.ASCII.GetBytes(numAsStr);
        AssertExtensions.Equal(expected, reader.Buffer.Slice(0, numAsStr.Length).ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(_ulongMaxValueLength / 2)]
    [InlineData(_ulongMaxValueLength - 1)]
    public void WritesNumericAcrossSpanBoundaries(int gapSize)
    {
        var writerBuffer = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(writerBuffer);
        // almost fill up the first block
        var spacer = new byte[writer.Span.Length - gapSize];
        writer.Write(spacer);

        var bufferLength = writer.Span.Length;
        writer.WriteNumeric(ulong.MaxValue);
        Assert.NotEqual(bufferLength, writer.Span.Length);
        writer.Commit();
        writerBuffer.FlushAsync().GetAwaiter().GetResult();

        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        var numAsString = ulong.MaxValue.ToString(CultureInfo.InvariantCulture);
        var written = reader.Buffer.Slice(spacer.Length, numAsString.Length);
        Assert.False(written.IsSingleSegment, "The buffer should cross spans");
        AssertExtensions.Equal(Encoding.ASCII.GetBytes(numAsString), written.ToArray());
    }

    [Theory]
    [InlineData("\0abcxyz", new byte[] { 0, 97, 98, 99, 120, 121, 122 })]
    [InlineData("!#$%i", new byte[] { 33, 35, 36, 37, 105 })]
    [InlineData("!#$%", new byte[] { 33, 35, 36, 37 })]
    [InlineData("!#$", new byte[] { 33, 35, 36 })]
    [InlineData("!#", new byte[] { 33, 35 })]
    [InlineData("!", new byte[] { 33 })]
    // null or empty
    [InlineData("", new byte[0])]
    [InlineData(null, new byte[0])]
    public void EncodesAsAscii(string input, byte[] expected)
    {
        var pipeWriter = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(pipeWriter);
        writer.WriteAscii(input);
        writer.Commit();
        pipeWriter.FlushAsync().GetAwaiter().GetResult();
        pipeWriter.Complete();

        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();

        if (expected.Length > 0)
        {
            AssertExtensions.Equal(
                expected,
                reader.Buffer.ToArray());
        }
        else
        {
            Assert.Equal(0, reader.Buffer.Length);
        }
    }

    [Theory]
    // non-ascii characters stored in 32 bits
    [InlineData("𤭢𐐝")]
    // non-ascii characters stored in 16 bits
    [InlineData("ñ٢⛄⛵")]
    public void WriteAsciiWritesOnlyOneBytePerChar(string input)
    {
        // WriteAscii doesn't validate if characters are in the ASCII range
        // but it shouldn't produce more than one byte per character
        var writerBuffer = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(writerBuffer);
        writer.WriteAscii(input);
        writer.Commit();
        writerBuffer.FlushAsync().GetAwaiter().GetResult();
        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();

        Assert.Equal(input.Length, reader.Buffer.Length);
    }

    [Fact]
    public void WriteAscii()
    {
        const byte maxAscii = 0x7f;
        var writerBuffer = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(writerBuffer);
        for (var i = 0; i < maxAscii; i++)
        {
            writer.WriteAscii(new string((char)i, 1));
        }
        writer.Commit();
        writerBuffer.FlushAsync().GetAwaiter().GetResult();

        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        var data = reader.Buffer.Slice(0, maxAscii).ToArray();
        for (var i = 0; i < maxAscii; i++)
        {
            Assert.Equal(i, data[i]);
        }
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(7, 4)]
    [InlineData(8, 3)]
    [InlineData(8, 4)]
    [InlineData(8, 5)]
    [InlineData(100, 48)]
    public void WritesAsciiAcrossBlockBoundaries(int stringLength, int gapSize)
    {
        var testString = new string(' ', stringLength);
        var writerBuffer = _pipe.Writer;
        var writer = new BufferWriter<PipeWriter>(writerBuffer);
        // almost fill up the first block
        var spacer = new byte[writer.Span.Length - gapSize];
        writer.Write(spacer);
        Assert.Equal(gapSize, writer.Span.Length);

        var bufferLength = writer.Span.Length;
        writer.WriteAscii(testString);
        Assert.NotEqual(bufferLength, writer.Span.Length);
        writer.Commit();
        writerBuffer.FlushAsync().GetAwaiter().GetResult();

        var reader = _pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        var written = reader.Buffer.Slice(spacer.Length, stringLength);
        Assert.False(written.IsSingleSegment, "The buffer should cross spans");
        AssertExtensions.Equal(Encoding.ASCII.GetBytes(testString), written.ToArray());
    }
}
