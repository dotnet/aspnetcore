// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class WriteOnlyPipeStreamTests : PipeStreamTest
    {
        [Fact]
        public void CanSeekFalse()
        {
            Assert.False(WritingStream.CanSeek);
        }

        [Fact]
        public void CanReadFalse()
        {
            Assert.False(WritingStream.CanRead);
        }

        [Fact]
        public void CanWriteTrue()
        {
            Assert.True(WritingStream.CanWrite);
        }

        [Fact]
        public void LengthThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.Length);
        }

        [Fact]
        public void PositionThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.Position);
            Assert.Throws<NotSupportedException>(() => WritingStream.Position = 1);
        }

        [Fact]
        public void SeekThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLengthThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.SetLength(1));
        }

        [Fact]
        public void ReadThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public async Task ReadAsyncThrows()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await WritingStream.ReadAsync(new byte[1], 0, 1));
        }

        [Fact]
        public void ReadTimeoutThrows()
        {
            Assert.Throws<NotSupportedException>(() => WritingStream.ReadTimeout = 1);
            Assert.Throws<NotSupportedException>(() => WritingStream.ReadTimeout);
        }

        [Fact]
        public async Task WriteAsyncWithReadOnlyMemoryWorks()
        {
            var expected = "Hello World!";

            await WriteStringToStreamAsync(expected);

            Assert.Equal(expected, await ReadFromPipeAsStringAsync());
        }

        [Fact]
        public async Task WriteAsyncWithArrayWorks()
        {
            var expected = new byte[1];

            await WritingStream.WriteAsync(expected, 0, expected.Length);

            Assert.Equal(expected, await ReadFromPipeAsByteArrayAsync());
        }

        [Fact]
        public async Task BasicLargeWrite()
        {
            var expected = new byte[8000];

            await WritingStream.WriteAsync(expected);

            Assert.Equal(expected, await ReadFromPipeAsByteArrayAsync());
        } 

        [Fact]
        public void FlushAsyncIsCalledFromCallingFlush()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);

            stream.Flush();

            pipeWriter.Verify(m => m.FlushAsync(default));
        }

        [Fact]
        public async Task FlushAsyncIsCalledFromCallingFlushAsync()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);

            await stream.FlushAsync();

            pipeWriter.Verify(m => m.FlushAsync(default));
        }

        [Fact]
        public async Task FlushAsyncCancellationTokenIsPassedIntoFlushAsync()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);
            var token = new CancellationToken();

            await stream.FlushAsync(token);

            pipeWriter.Verify(m => m.FlushAsync(token));
        }

        [Fact]
        public void WriteAsyncIsCalledFromCallingWrite()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);

            stream.Write(new byte[1]);

            pipeWriter.Verify(m => m.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WriteAsyncIsCalledFromCallingWriteAsync()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);

            await stream.WriteAsync(new byte[1]);

            pipeWriter.Verify(m => m.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WriteAsyncCancellationTokenIsPassedIntoWriteAsync()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);
            var token = new CancellationToken();

            await stream.WriteAsync(new byte[1], token);

            pipeWriter.Verify(m => m.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), token));
        }

        [Fact]
        public void WriteAsyncIsCalledFromBeginWrite()
        {
            var pipeWriter = new Mock<PipeWriter>();
            var stream = new WriteOnlyPipeStream(pipeWriter.Object);
            stream.BeginWrite(new byte[1], 0, 1, null, this);
            pipeWriter.Verify(m => m.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task BeginAndEndWriteWork()
        {
            var expected = new byte[1];
            var asyncResult = WritingStream.BeginWrite(expected, 0, 1, null, this);
            WritingStream.EndWrite(asyncResult);
            Assert.Equal(expected, await ReadFromPipeAsByteArrayAsync());
        }

        [Fact]
        public void BlockSyncIOThrows()
        {
            var writeOnlyPipeStream = new WriteOnlyPipeStream(Writer, allowSynchronousIO: false);
            Assert.Throws<InvalidOperationException>(() => writeOnlyPipeStream.Write(new byte[0], 0, 0));
            Assert.Throws<InvalidOperationException>(() => writeOnlyPipeStream.Flush());
        }

        [Fact]
        public void InnerPipeWriterReturnsPipeWriter()
        {
            var writeOnlyPipeStream = new WriteOnlyPipeStream(Writer, allowSynchronousIO: false);
            Assert.Equal(Writer, writeOnlyPipeStream.InnerPipeWriter);
        }
    }
}
