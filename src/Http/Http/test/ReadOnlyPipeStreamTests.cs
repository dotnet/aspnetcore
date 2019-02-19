// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadOnlyPipeStreamTests : PipeStreamTest
    {
        [Fact]
        public void CanSeekFalse()
        {
            Assert.False(ReadingStream.CanSeek);
        }

        [Fact]
        public void CanReadTrue()
        {
            Assert.True(ReadingStream.CanRead);
        }

        [Fact]
        public void CanWriteFalse()
        {
            Assert.False(ReadingStream.CanWrite);
        }

        [Fact]
        public void LengthThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.Length);
        }

        [Fact]
        public void PositionThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.Position);
            Assert.Throws<NotSupportedException>(() => ReadingStream.Position = 1);
        }

        [Fact]
        public void SeekThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void SetLengthThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.SetLength(1));
        }

        [Fact]
        public void WriteThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public async Task WriteAsyncThrows()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await ReadingStream.WriteAsync(new byte[1], 0, 1));
        }

        [Fact]
        public void ReadTimeoutThrows()
        {
            Assert.Throws<NotSupportedException>(() => ReadingStream.WriteTimeout = 1);
            Assert.Throws<NotSupportedException>(() => ReadingStream.WriteTimeout);
        }

        [Fact]
        public async Task ReadAsyncWorks()
        {
            var expected = "Hello World!";

            await WriteStringToPipeAsync(expected);

            Assert.Equal(expected, await ReadFromStreamAsStringAsync());
        }

        [Fact]
        public async Task BasicLargeRead()
        {
            var expected = new byte[8000];

            await WriteByteArrayToPipeAsync(expected);

            Assert.Equal(expected, await ReadFromStreamAsByteArrayAsync(8000));
        }

        [Fact]
        public async Task ReadAsyncIsCalledFromCallingRead()
        {
            var pipeReader = await SetupMockPipeReader();
            var stream = new ReadOnlyPipeStream(pipeReader.Object);

            stream.Read(new byte[1]);

            pipeReader.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ReadAsyncIsCalledFromCallingReadAsync()
        {
            var pipeReader = await SetupMockPipeReader();
            var stream = new ReadOnlyPipeStream(pipeReader.Object);

            await stream.ReadAsync(new byte[1]);

            pipeReader.Verify(m => m.ReadAsync(It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ReadAsyncCancellationTokenIsPassedIntoReadAsync()
        {
            var pipeReader = await SetupMockPipeReader();
            var stream = new ReadOnlyPipeStream(pipeReader.Object);
            var token = new CancellationToken();

            await stream.ReadAsync(new byte[1], token);

            pipeReader.Verify(m => m.ReadAsync(token));
        }

        [Fact]
        public async Task CopyToAsyncWorks()
        {
            const int expectedSize = 8000;
            var expected = new byte[expectedSize];

            await WriteByteArrayToPipeAsync(expected);

            Writer.Complete();
            var destStream = new MemoryStream();

            await ReadingStream.CopyToAsync(destStream);

            Assert.Equal(expectedSize, destStream.Length);
        }

        [Fact]
        public void BlockSyncIOThrows()
        {
            var readOnlyPipeStream = new ReadOnlyPipeStream(Reader, allowSynchronousIO: false);
            Assert.Throws<InvalidOperationException>(() => readOnlyPipeStream.Read(new byte[0], 0, 0));
        }

        [Fact]
        public void InnerPipeReaderReturnsPipeReader()
        {
            var readOnlyPipeStream = new ReadOnlyPipeStream(Reader, allowSynchronousIO: false);
            Assert.Equal(Reader, readOnlyPipeStream.InnerPipeReader);
        }

        [Fact]
        public async Task ThrowsOperationCanceledExceptionIfCancelPendingReadWasCalledOnInnerPipeReader()
        {
            var readOnlyPipeStream = new ReadOnlyPipeStream(Reader);
            var readOperation = readOnlyPipeStream.ReadAsync(new byte[1]);

            Assert.False(readOperation.IsCompleted);

            Reader.CancelPendingRead();

            var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await readOperation);

            Assert.Equal(ThrowHelper.CreateOperationCanceledException_ReadCanceled().Message, ex.Message);
        }

        private async Task<Mock<PipeReader>> SetupMockPipeReader()
        {
            await WriteByteArrayToPipeAsync(new byte[1]);

            var pipeReader = new Mock<PipeReader>();
            pipeReader
                .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(new byte[1]), false, false)));
            return pipeReader;
        }
    }
}
