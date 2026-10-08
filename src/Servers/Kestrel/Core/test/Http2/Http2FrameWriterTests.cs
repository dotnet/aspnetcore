// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2FrameWriterTests
{
    private readonly MemoryPool<byte> _dirtyMemoryPool;

    public Http2FrameWriterTests()
    {
        var memoryBlock = new Mock<IMemoryOwner<byte>>();
        memoryBlock.Setup(block => block.Memory).Returns(() =>
        {
            var blockArray = new byte[4096];
            for (int i = 0; i < 4096; i++)
            {
                blockArray[i] = 0xff;
            }
            return new Memory<byte>(blockArray);
        });

        var dirtyMemoryPool = new Mock<MemoryPool<byte>>();
        dirtyMemoryPool.Setup(pool => pool.Rent(It.IsAny<int>())).Returns(memoryBlock.Object);
        _dirtyMemoryPool = dirtyMemoryPool.Object;
    }

    [Fact]
    public async Task WriteWindowUpdate_UnsetsReservedBit()
    {
        // Arrange
        var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
        var frameWriter = CreateFrameWriter(pipe);

        // Act
        await frameWriter.WriteWindowUpdateAsync(1, 1);

        // Assert
        var payload = await pipe.Reader.ReadForLengthAsync(Http2FrameReader.HeaderLength + 4);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, payload.Skip(Http2FrameReader.HeaderLength).Take(4).ToArray());
    }

    private Http2FrameWriter CreateFrameWriter(Pipe pipe)
    {
        var serviceContext = TestContextFactory.CreateServiceContext(new KestrelServerOptions());
        return new Http2FrameWriter(pipe.Writer, null, null, 1, null, null, null, _dirtyMemoryPool, serviceContext);
    }

    [Fact]
    public async Task WriteGoAway_UnsetsReservedBit()
    {
        // Arrange
        var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
        var frameWriter = CreateFrameWriter(pipe);

        // Act
        await frameWriter.WriteGoAwayAsync(1, Http2ErrorCode.NO_ERROR);

        // Assert
        var payload = await pipe.Reader.ReadForLengthAsync(Http2FrameReader.HeaderLength + 4);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, payload.Skip(Http2FrameReader.HeaderLength).Take(4).ToArray());
    }

    [Fact]
    public async Task WriteHeader_UnsetsReservedBit()
    {
        // Arrange
        var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
        var frame = new Http2Frame();
        frame.PreparePing(Http2PingFrameFlags.NONE);

        // Act
        Http2FrameWriter.WriteHeader(frame, pipe.Writer);
        await pipe.Writer.FlushAsync();

        // Assert
        var payload = await pipe.Reader.ReadForLengthAsync(Http2FrameReader.HeaderLength);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, payload.Skip(5).Take(4).ToArray());
    }

    [Fact]
    public void UpdateMaxFrameSize_To_ProtocolMaximum()
    {
        var sut = CreateFrameWriter(new Pipe());
        sut.UpdateMaxFrameSize((int)Math.Pow(2, 24) - 1);
    }

    [Fact]
    public void UpdateMaxFrameSize_To_SmallerSize_DoesNotReplaceHeaderEncodingBuffer()
    {
        var sut = CreateFrameWriter(new Pipe());
        sut.UpdateMaxFrameSize((int)Http2PeerSettings.MaxAllowedMaxFrameSize);

        var headerEncodingBuffer = GetHeaderEncodingBuffer(sut);

        sut.UpdateMaxFrameSize((int)Http2PeerSettings.MinAllowedMaxFrameSize);

        Assert.Equal((int)Http2PeerSettings.MinAllowedMaxFrameSize, GetMaxFrameSize(sut));
        Assert.Same(headerEncodingBuffer, GetHeaderEncodingBuffer(sut));
    }

    [Fact]
    public async Task WriteResponseHeaders_AfterMaxFrameSizeDecreases_DoesNotExceedFrameSize()
    {
        var pipe = new Pipe();
        var sut = CreateFrameWriter(pipe);
        sut.UpdateMaxFrameSize((int)Http2PeerSettings.MaxAllowedMaxFrameSize);
        sut.UpdateMaxFrameSize((int)Http2PeerSettings.MinAllowedMaxFrameSize);

        IHeaderDictionary headers = new HttpResponseHeaders();
        headers["Custom"] = new string('a', 64 * 1024);

        sut.WriteResponseHeaders(1, StatusCodes.Status200OK, Http2HeadersFrameFlags.NONE, (HttpResponseHeaders)headers);
        var flushTask = sut.WriteSettingsAckAsync();

        var result = await pipe.Reader.ReadAsync();
        var frameHeader = result.Buffer.Slice(0, Http2FrameReader.HeaderLength).ToArray();
        pipe.Reader.AdvanceTo(result.Buffer.End);
        await flushTask;
        var payloadLength = (frameHeader[0] << 16) | (frameHeader[1] << 8) | frameHeader[2];

        Assert.Equal((byte)Http2FrameType.HEADERS, frameHeader[3]);
        Assert.InRange(payloadLength, 0, (int)Http2PeerSettings.MinAllowedMaxFrameSize);
    }

    private static byte[] GetHeaderEncodingBuffer(Http2FrameWriter frameWriter)
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        return (byte[])typeof(Http2FrameWriter).GetField("_headerEncodingBuffer", PrivateInstance)!.GetValue(frameWriter)!;
    }

    private static int GetMaxFrameSize(Http2FrameWriter frameWriter)
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        return (int)typeof(Http2FrameWriter).GetField("_maxFrameSize", PrivateInstance)!.GetValue(frameWriter)!;
    }
}

public static class PipeReaderExtensions
{
    public static async Task<byte[]> ReadForLengthAsync(this PipeReader pipeReader, int length)
    {
        while (true)
        {
            var result = await pipeReader.ReadAsync();
            var buffer = result.Buffer;

            if (!buffer.IsEmpty && buffer.Length >= length)
            {
                return buffer.Slice(0, length).ToArray();
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }
}
