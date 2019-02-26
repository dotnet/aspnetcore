// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public partial class StreamPipeReaderTests : StreamPipeTest
    {
        [Fact]
        public async Task CanRead()
        {
            Write(Encoding.ASCII.GetBytes("Hello World"));
            var readResult = await Reader.ReadAsync();
            var buffer = readResult.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));
            Reader.AdvanceTo(buffer.End);
        }

        [Fact]
        public async Task CanReadMultipleTimes()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));
            var readResult = await Reader.ReadAsync();

            Assert.Equal(MinimumSegmentSize, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(MinimumSegmentSize * 2, readResult.Buffer.Length);
            Assert.False(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(10000, readResult.Buffer.Length);
            Assert.False(readResult.Buffer.IsSingleSegment);

            Reader.AdvanceTo(readResult.Buffer.End);
        }

        [Fact]
        public async Task ReadWithAdvance()
        {
            WriteByteArray(9000);

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(MinimumSegmentSize, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);
        }

        [Fact]
        public async Task ReadWithAdvanceDifferentSegmentSize()
        {
            CreateReader(minimumSegmentSize: 4095);

            WriteByteArray(9000);

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(4095, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);
        }

        [Fact]
        public async Task ReadWithAdvanceSmallSegments()
        {
            CreateReader();

            WriteByteArray(128);

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.End);

            readResult = await Reader.ReadAsync();
            Assert.Equal(16, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);
        }

        [Fact]
        public async Task ReadConsumePartialReadAsyncCallsTryRead()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.GetPosition(2048));

            // Confirm readResults are the same.
            var readResult2 = await Reader.ReadAsync();

            var didRead = Reader.TryRead(out var readResult3);

            Assert.Equal(readResult2, readResult3);
        }

        [Fact]
        public async Task ReadConsumeEntireTryReadReturnsNothing()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.End);
            var didRead = Reader.TryRead(out readResult);

            Assert.False(didRead);
        }

        [Fact]
        public async Task ReadExaminePartialReadAsyncDoesNotReturnMoreBytes()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.GetPosition(2048));

            var readResult2 = await Reader.ReadAsync();

            Assert.Equal(readResult, readResult2);
        }

        [Fact]
        public async Task ReadExamineEntireReadAsyncReturnsNewData()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            var readResult2 = await Reader.ReadAsync();
            Assert.NotEqual(readResult, readResult2);
        }

        [Fact]
        public async Task ReadCanBeCancelledViaProvidedCancellationToken()
        {
            var pipeReader = new StreamPipeReader(new HangingStream());
            var cts = new CancellationTokenSource(1);
            await Task.Delay(1);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await pipeReader.ReadAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCanBeCanceledViaCancelPendingReadWhenReadIsAsync()
        {
            var pipeReader = new StreamPipeReader(new HangingStream());

            var result = new ReadResult();
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = Task.Run(async () =>
            {
                var readingTask = pipeReader.ReadAsync();
                tcs.SetResult(0);
                result = await readingTask;
            });
            await tcs.Task;
            pipeReader.CancelPendingRead();
            await task;

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task ReadAsyncReturnsCanceledIfCanceledBeforeRead()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            // Make sure state isn't used from before
            for (var i = 0; i < 3; i++)
            {
                Reader.CancelPendingRead();
                var readResultTask = Reader.ReadAsync();
                Assert.True(readResultTask.IsCompleted);
                var readResult = readResultTask.GetAwaiter().GetResult();
                Assert.True(readResult.IsCanceled);
                readResult = await Reader.ReadAsync();
                Reader.AdvanceTo(readResult.Buffer.End);
            }
        }

        [Fact]
        public async Task ReadAsyncReturnsCanceledInterleaved()
        {
            Write(new byte[10000]);

            // Cancel and Read interleaved to confirm cancellations are independent
            for (var i = 0; i < 3; i++)
            {
                Reader.CancelPendingRead();
                var readResultTask = Reader.ReadAsync();
                Assert.True(readResultTask.IsCompleted);
                var readResult = readResultTask.GetAwaiter().GetResult();
                Assert.True(readResult.IsCanceled);

                readResult = await Reader.ReadAsync();
                Assert.False(readResult.IsCanceled);
            }
        }

        [Fact]
        public async Task AdvanceWithEmptySequencePositionNoop()
        {
            Write(Encoding.ASCII.GetBytes(new string('a', 10000)));

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start);
            var readResult2 = await Reader.ReadAsync();

            Assert.Equal(readResult, readResult2);
        }

        [Fact]
        public async Task AdvanceToInvalidCursorThrows()
        {
            Write(new byte[100]);

            var result = await Reader.ReadAsync();
            var buffer = result.Buffer;

            Reader.AdvanceTo(buffer.End);

            Reader.CancelPendingRead();
            result = await Reader.ReadAsync();
            Assert.Throws<ArgumentOutOfRangeException>(() => Reader.AdvanceTo(buffer.End));
            Reader.AdvanceTo(result.Buffer.End);
        }

        [Fact]
        public void AdvanceWithoutReadingWithValidSequencePosition()
        {
            var sequencePosition = new SequencePosition(new BufferSegment(), 5);
            Assert.Throws<InvalidOperationException>(() => Reader.AdvanceTo(sequencePosition));
        }

        [Fact]
        public async Task AdvanceMultipleSegments()
        {
            CreateReader();

            WriteByteArray(128);

            var result = await Reader.ReadAsync();
            Assert.Equal(16, result.Buffer.Length);
            Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            var result2 = await Reader.ReadAsync();
            Assert.Equal(32, result2.Buffer.Length);
            Reader.AdvanceTo(result.Buffer.End, result2.Buffer.End);

            var result3 = await Reader.ReadAsync();
            Assert.Equal(32, result3.Buffer.Length);
        }

        [Fact]
        public async Task AdvanceMultipleSegmentsEdgeCase()
        {
            CreateReader();

            WriteByteArray(128);

            var result = await Reader.ReadAsync();
            Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            result = await Reader.ReadAsync();
            Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            var result2 = await Reader.ReadAsync();
            Assert.Equal(48, result2.Buffer.Length);
            Reader.AdvanceTo(result.Buffer.End, result2.Buffer.End);

            var result3 = await Reader.ReadAsync();
            Assert.Equal(32, result3.Buffer.Length);
        }

        [Fact]
        public async Task CompleteReaderWithoutAdvanceDoesNotThrow()
        {
            WriteByteArray(100);
            await Reader.ReadAsync();
            Reader.Complete();
        }

        [Fact]
        public async Task AdvanceAfterCompleteThrows()
        {
            WriteByteArray(100);
            var buffer = (await Reader.ReadAsync()).Buffer;

            Reader.Complete();

            var exception = Assert.Throws<InvalidOperationException>(() => Reader.AdvanceTo(buffer.End));
            Assert.Equal("Reading is not allowed after reader was completed.", exception.Message);
        }

        [Fact]
        public async Task ReadBetweenBlocks()
        {
            var blockSize = 16;
            CreateReader();

            WriteWithoutPosition(Enumerable.Repeat((byte)'a', blockSize - 5).ToArray());
            Write(Encoding.ASCII.GetBytes("Hello World"));

            // ReadAsync will only return one chunk at a time, so Advance/ReadAsync to get two chunks
            var result = await Reader.ReadAsync();
            Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            result = await Reader.ReadAsync();

            var buffer = result.Buffer;
            Assert.False(buffer.IsSingleSegment);
            var helloBuffer = buffer.Slice(blockSize - 5);
            Assert.False(helloBuffer.IsSingleSegment);
            var memory = new List<ReadOnlyMemory<byte>>();
            foreach (var m in helloBuffer)
            {
                memory.Add(m);
            }

            var spans = memory;
            Reader.AdvanceTo(buffer.Start, buffer.Start);

            Assert.Equal(2, memory.Count);
            var helloBytes = new byte[spans[0].Length];
            spans[0].Span.CopyTo(helloBytes);
            var worldBytes = new byte[spans[1].Length];
            spans[1].Span.CopyTo(worldBytes);
            Assert.Equal("Hello", Encoding.ASCII.GetString(helloBytes));
            Assert.Equal(" World", Encoding.ASCII.GetString(worldBytes));
        }

        [Fact]
        public async Task ThrowsOnReadAfterCompleteReader()
        {
            Reader.Complete();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Reader.ReadAsync());
        }

        [Fact]
        public void TryReadAfterCancelPendingReadReturnsTrue()
        {
            Reader.CancelPendingRead();

            var gotData = Reader.TryRead(out var result);

            Assert.True(result.IsCanceled);

            Reader.AdvanceTo(result.Buffer.End);
        }

        [Fact]
        public void ReadAsyncWithDataReadyReturnsTaskWithValue()
        {
            WriteByteArray(20);
            var task = Reader.ReadAsync();
            Assert.True(IsTaskWithResult(task));
        }

        [Fact]
        public async Task ArrayPoolUsedByDefault()
        {
            WriteByteArray(20);
            var reader = new StreamPipeReader(Stream);
            var result = await reader.ReadAsync();

            SequenceMarshal.TryGetReadOnlySequenceSegment(
                result.Buffer,
                out var startSegment,
                out var startIndex,
                out var endSegment,
                out var endIndex);

            var start = (BufferSegment)startSegment;
            var end = (BufferSegment)endSegment;

            Assert.Same(start, end);
            Assert.IsType<byte[]>(start.MemoryOwner);

            reader.AdvanceTo(result.Buffer.End);
            reader.Complete();
        }

        [Fact]
        public void CancelledReadAsyncReturnsTaskWithValue()
        {
            Reader.CancelPendingRead();
            var task = Reader.ReadAsync();
            Assert.True(IsTaskWithResult(task));
        }

        [Fact]
        public async Task AdvancePastMinReadSizeReadAsyncReturnsMoreData()
        {
            CreateReader();

            WriteByteArray(32);
            var result = await Reader.ReadAsync();
            Assert.Equal(16, result.Buffer.Length);

            Reader.AdvanceTo(result.Buffer.GetPosition(12), result.Buffer.End);
            result = await Reader.ReadAsync();
            Assert.Equal(20, result.Buffer.Length);
        }

        [Fact]
        public async Task ExamineEverythingResetsAfterSuccessfulRead()
        {
            WriteByteArray(10000);

            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            var readResult2 = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult2.Buffer.GetPosition(2000));

            var readResult3 = await Reader.ReadAsync();
            Assert.Equal(6192, readResult3.Buffer.Length);
        }

        [Fact]
        public async Task ReadMultipleTimesAdvanceFreesAppropriately()
        {
            var pool = new TestMemoryPool();
            CreateReader(memoryPool: pool);

            WriteByteArray(2000);

            for (var i = 0; i < 99; i++)
            {
                var readResult = await Reader.ReadAsync();
                Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            }

            var result = await Reader.ReadAsync();
            Reader.AdvanceTo(result.Buffer.End);
            Assert.Equal(1, pool.GetRentCount());
        }

        [Fact]
        public async Task AsyncReadWorks()
        {
            Stream = new AsyncStream();
            CreateReader();

            WriteByteArray(2000);

            for (var i = 0; i < 99; i++)
            {
                var readResult = await Reader.ReadAsync();
                Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            }

            var result = await Reader.ReadAsync();
            Assert.Equal(1600, result.Buffer.Length);
            Reader.AdvanceTo(result.Buffer.End);
        }

        [Fact]
        public async Task ConsumePartialBufferWorks()
        {
            CreateReader();

            Write(Encoding.ASCII.GetBytes(new string('a', 8)));
            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.GetPosition(4), readResult.Buffer.End);
            Stream.Position = 0;

            readResult = await Reader.ReadAsync();
            var resultString = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            Assert.Equal(new string('a', 12), resultString);
            Reader.AdvanceTo(readResult.Buffer.End);
        }

        [Fact]
        public async Task ConsumePartialBufferBetweenMultipleSegmentsWorks()
        {
            CreateReader();

            Write(Encoding.ASCII.GetBytes(new string('a', 8)));
            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.GetPosition(4), readResult.Buffer.End);
            Stream.Position = 0;

            readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
            Stream.Position = 0;

            readResult = await Reader.ReadAsync();
            var resultString = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            Assert.Equal(new string('a', 20), resultString);

            Reader.AdvanceTo(readResult.Buffer.End);
        }

        [Fact]
        public async Task SetMinimumReadThresholdSegmentAdvancesCorrectly()
        {
            CreateReader(minimumReadThreshold: 8);

            WriteByteArray(9);
            var readResult = await Reader.ReadAsync();
            Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            AppendByteArray(9);
            readResult = await Reader.ReadAsync();

            foreach (var segment in readResult.Buffer)
            {
                Assert.Equal(9, segment.Length);
            }
            Assert.False(readResult.Buffer.IsSingleSegment);
        }

        [Fact]
        public void SetMinimumReadThresholdOfZeroThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeReader(Stream,
                new StreamPipeReaderOptions(minimumSegmentSize: 4096, minimumReadThreshold: 0, new TestMemoryPool())));
        }

        [Fact]
        public void SetOptionsToNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new StreamPipeReader(Stream, null));
        }

        [Fact]
        public async Task UseBothStreamAndPipeToReadConfirmSameSize()
        {
            Write(new byte[8]);
            var buffer = new byte[4];

            Stream.Read(buffer, 0, buffer.Length);
            var readResult = await Reader.ReadAsync();

            Assert.Equal(buffer, readResult.Buffer.ToArray());
        }

        [Fact]
        public async Task UseStreamThenPipeToReadNoBytesLost()
        {
            CreateReader(minimumSegmentSize: 1, minimumReadThreshold: 1);

            var expectedString = WriteString("abcdef");
            var accumulatedResult = "";
            var buffer = new byte[1];

            for (var i = 0; i < expectedString.Length / 2; i++)
            {
                // Read from stream then pipe to guarantee no bytes are lost.
                accumulatedResult += ReadFromStreamAsString(buffer);
                accumulatedResult += await ReadFromPipeAsString();
            }

            Assert.Equal(expectedString, accumulatedResult);
        }

        [Fact]
        public async Task UsePipeThenStreamToReadNoBytesLost()
        {
            CreateReader(minimumSegmentSize: 1, minimumReadThreshold: 1);

            var expectedString = WriteString("abcdef");
            var accumulatedResult = "";
            var buffer = new byte[1];

            for (var i = 0; i < expectedString.Length / 2; i++)
            {
                // Read from pipe then stream to guarantee no bytes are lost.
                accumulatedResult += await ReadFromPipeAsString();
                accumulatedResult += ReadFromStreamAsString(buffer);
            }

            Assert.Equal(expectedString, accumulatedResult);
        }

        [Fact]
        public async Task UseBothStreamAndPipeToReadWithoutAdvance_StreamIgnoresAdvance()
        {
            var buffer = new byte[1];
            CreateReader(minimumSegmentSize: 1, minimumReadThreshold: 1);

            WriteString("abc");
            ReadFromStreamAsString(buffer);
            var readResult = await Reader.ReadAsync();

            // No Advance
            // Next call to Stream.Read will get the next 4 bytes rather than the bytes already read by the pipe
            Assert.Equal("c", ReadFromStreamAsString(buffer));
        }

        [Fact]
        public async Task ReadAsyncWithNoDataCompletesReader()
        {
            var readResult = await Reader.ReadAsync();

            Assert.True(readResult.IsCompleted);
        }

        [Fact]
        public async Task ReadAsyncWithEmptyDataCompletesStream()
        {
            WriteByteArray(0);

            var readResult = await Reader.ReadAsync();

            Assert.True(readResult.IsCompleted);
        }

        [Fact]
        public async Task ReadAsyncAfterReceivingCompletedReadResultDoesNotThrow()
        {
            Stream = new ThrowAfterZeroByteReadStream();
            Reader = new StreamPipeReader(Stream);
            var readResult = await Reader.ReadAsync();

            readResult = await Reader.ReadAsync();
            Assert.True(readResult.Buffer.IsEmpty);
            Assert.True(readResult.IsCompleted);
        }

        [Fact]
        public void InnerStreamReturnsStream()
        {
            Assert.Equal(Stream, ((StreamPipeReader)Reader).InnerStream);
        }

        [Fact]
        public async Task BufferingDataPastEndOfStreamCanBeReadAgain()
        {
            var helloBytes = Encoding.ASCII.GetBytes("Hello World");
            Write(helloBytes);

            var readResult = await Reader.ReadAsync();
            var buffer = readResult.Buffer;
            Reader.AdvanceTo(buffer.Start, buffer.End);

            // Make sure IsCompleted is true
            readResult = await Reader.ReadAsync();
            buffer = readResult.Buffer;
            Reader.AdvanceTo(buffer.Start, buffer.End);
            Assert.True(readResult.IsCompleted);

            var value = await ReadFromPipeAsString();
            Assert.Equal("Hello World", value);
        }

        private async Task<string> ReadFromPipeAsString()
        {
            var readResult = await Reader.ReadAsync();
            
            var result = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            Reader.AdvanceTo(readResult.Buffer.End);
            return result;
        }

        private string ReadFromStreamAsString(byte[] buffer)
        {
            var res = Stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer);
        }

        private string WriteString(string expectedString)
        {
            Write(Encoding.ASCII.GetBytes(expectedString));
            return expectedString;
        }

        private void CreateReader(int minimumSegmentSize = 16, int minimumReadThreshold = 4, MemoryPool<byte> memoryPool = null)
        {
            Reader = new StreamPipeReader(Stream,
                new StreamPipeReaderOptions(
                    minimumSegmentSize,
                    minimumReadThreshold,
                    memoryPool ?? new TestMemoryPool()));
        }

        private bool IsTaskWithResult<T>(ValueTask<T> task)
        {
            return task == new ValueTask<T>(task.Result);
        }

        private void WriteByteArray(int size)
        {
            Write(new byte[size]);
        }

        private void AppendByteArray(int size)
        {
            Append(new byte[size]);
        }

        private class AsyncStream : MemoryStream
        {
            private static byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return await base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            // Keeping as this code will eventually be ported to corefx
#if NETCOREAPP3_0
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return await base.ReadAsync(buffer, cancellationToken);
            }
#endif
        }

        private class ThrowAfterZeroByteReadStream : MemoryStream
        {
            private bool _throwOnNextCallToRead;
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return ReadAsync(new Memory<byte>(buffer, offset, count)).AsTask();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                if (_throwOnNextCallToRead)
                {
                    throw new Exception();
                }
                var bytes = await base.ReadAsync(destination, cancellationToken);
                if (bytes == 0)
                {
                    _throwOnNextCallToRead = true;
                }
                return bytes;
            }
        }
    }
}
