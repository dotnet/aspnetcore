// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.InternalTesting;
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
