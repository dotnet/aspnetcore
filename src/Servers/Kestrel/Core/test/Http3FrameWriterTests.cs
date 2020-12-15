// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3FrameWriterTests
    {
        private MemoryPool<byte> _dirtyMemoryPool;

        public Http3FrameWriterTests()
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
        public async Task WriteSettings_NoSettingsWrittenWithProtocolDefault()
        {
            var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
            var frameWriter = new Http3FrameWriter(pipe.Writer, null, null, null, null, _dirtyMemoryPool, null);

            var settings = new Http3PeerSettings();
            await frameWriter.WriteSettingsAsync(settings.GetNonProtocolDefaults());

            var payload = await pipe.Reader.ReadForLengthAsync(2);

            Assert.Equal(new byte[] { 0x04, 0x00 }, payload.ToArray());
        }

        [Fact]
        public async Task WriteSettings_OneSettingsWrittenWithKestrelDefaults()
        {
            var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
            var frameWriter = new Http3FrameWriter(pipe.Writer, null, null, null, null, _dirtyMemoryPool, null);

            var limits = new Http3Limits();
            var settings = new Http3PeerSettings();
            settings.HeaderTableSize = (uint)limits.HeaderTableSize;
            settings.MaxRequestHeaderFieldSize = (uint)limits.MaxRequestHeaderFieldSize;

            await frameWriter.WriteSettingsAsync(settings.GetNonProtocolDefaults());

            // variable length ints make it so the results isn't know without knowing the values 
            var payload = await pipe.Reader.ReadForLengthAsync(5);

            Assert.Equal(new byte[] { 0x04, 0x03, 0x06, 0x60, 0x00 }, payload.ToArray());
        }

        [Fact]
        public async Task WriteSettings_TwoSettingsWritten()
        {
            var pipe = new Pipe(new PipeOptions(_dirtyMemoryPool, PipeScheduler.Inline, PipeScheduler.Inline));
            var frameWriter = new Http3FrameWriter(pipe.Writer, null, null, null, null, _dirtyMemoryPool, null);

            var settings = new Http3PeerSettings();
            settings.HeaderTableSize = 1234;
            settings.MaxRequestHeaderFieldSize = 567890;

            await frameWriter.WriteSettingsAsync(settings.GetNonProtocolDefaults());

            // variable length ints make it so the results isn't know without knowing the values 
            var payload = await pipe.Reader.ReadForLengthAsync(10);

            Assert.Equal(new byte[] { 0x04, 0x08, 0x01, 0x44, 0xD2, 0x06, 0x80, 0x08, 0xAA, 0x52 }, payload.ToArray());
        }
    }
}
