// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeWriterTests : StreamPipeTest
    {
        [Fact]
        public async Task CanWriteAsyncMultipleTimesIntoSameBlock()
        {
            await Writer.WriteAsync(new byte[] { 1 });
            await Writer.WriteAsync(new byte[] { 2 });
            await Writer.WriteAsync(new byte[] { 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Theory]
        [InlineData(100)]
        [InlineData(4000)]
        public async Task CanAdvanceWithPartialConsumptionOfFirstSegment(int firstWriteLength)
        {
            Writer = new StreamPipeWriter(Stream, MinimumSegmentSize, new TestMemoryPool(maxBufferSize: 20000));
            await Writer.WriteAsync(Encoding.ASCII.GetBytes("a"));

            var memory = Writer.GetMemory(firstWriteLength);
            Writer.Advance(firstWriteLength);

            memory = Writer.GetMemory();
            Writer.Advance(memory.Length);

            await Writer.FlushAsync();

            Assert.Equal(firstWriteLength + memory.Length + 1, Read().Length);
        }

        [Fact]
        public async Task WriteCanBeCancelledViaProvidedCancellationToken()
        {
            var pipeWriter = new StreamPipeWriter(new HangingStream());
            var cts = new CancellationTokenSource(1);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await pipeWriter.WriteAsync(Encoding.ASCII.GetBytes("data"), cts.Token));
        }

        [Fact]
        public async Task WriteCanBeCanceledViaCancelPendingFlushWhenFlushIsAsync()
        {
            var pipeWriter = new StreamPipeWriter(new HangingStream());
            FlushResult flushResult = new FlushResult();

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = Task.Run(async () =>
            {
                try
                {
                    var writingTask = pipeWriter.WriteAsync(Encoding.ASCII.GetBytes("data"));
                    tcs.SetResult(0);
                    flushResult = await writingTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            });

            await tcs.Task;

            pipeWriter.CancelPendingFlush();

            await task;

            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public void FlushAsyncCompletedAfterPreCancellation()
        {
            PipeWriter writableBuffer = Writer.WriteEmpty(1);

            Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);

            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();

            Assert.True(flushResult.IsCanceled);

            flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
        }

        [Fact]
        public async Task FlushAsyncReturnsCanceledIfCanceledBeforeFlush()
        {
            await CheckCanceledFlush();
        }

        [Fact]
        public async Task FlushAsyncReturnsCanceledIfCanceledBeforeFlushMultipleTimes()
        {
            for (var i = 0; i < 10; i++)
            {
                await CheckCanceledFlush();
            }
        }

        [Fact]
        public async Task FlushAsyncReturnsCanceledInterleaved()
        {
            for (var i = 0; i < 5; i++)
            {
                await CheckCanceledFlush();
                await CheckWriteIsNotCanceled();
            }
        }

        [Fact]
        public async Task CancelPendingFlushBetweenWritesAllDataIsPreserved()
        {
            Stream = new SingleWriteStream();
            Writer = new StreamPipeWriter(Stream);
            FlushResult flushResult = new FlushResult();

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = Task.Run(async () =>
            {
                try
                {
                    await Writer.WriteAsync(Encoding.ASCII.GetBytes("data"));

                    var writingTask = Writer.WriteAsync(Encoding.ASCII.GetBytes(" data"));
                    tcs.SetResult(0);
                    flushResult = await writingTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            });

            await tcs.Task;

            Writer.CancelPendingFlush();

            await task;

            Assert.True(flushResult.IsCanceled);

            await Writer.WriteAsync(Encoding.ASCII.GetBytes(" more data"));
            Assert.Equal(Encoding.ASCII.GetBytes("data data more data"), Read());
        }

        [Fact]
        public async Task CancelPendingFlushAfterAllWritesAllDataIsPreserved()
        {
            Stream = new CannotFlushStream();
            Writer = new StreamPipeWriter(Stream);
            FlushResult flushResult = new FlushResult();

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = Task.Run(async () =>
            {
                try
                {
                    // Create two Segments
                    // First one will succeed to write, other one will hang.
                    var writingTask = Writer.WriteAsync(Encoding.ASCII.GetBytes("data"));
                    tcs.SetResult(0);
                    flushResult = await writingTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            });

            await tcs.Task;

            Writer.CancelPendingFlush();

            await task;

            Assert.True(flushResult.IsCanceled);
        }

        [Fact]
        public async Task CancelPendingFlushLostOfCancellationsNoDataLost()
        {
            var writeSize = 16;
            var singleWriteStream = new SingleWriteStream();
            Stream = singleWriteStream;
            Writer = new StreamPipeWriter(Stream, minimumSegmentSize: writeSize);

            for (var i = 0; i < 10; i++)
            {
                FlushResult flushResult = new FlushResult();
                var expectedData = Encoding.ASCII.GetBytes(new string('a', writeSize));

                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Create two Segments
                        // First one will succeed to write, other one will hang.
                        for (var j = 0; j < 2; j++)
                        {
                            Writer.Write(expectedData);
                        }

                        var flushTask = Writer.FlushAsync();
                        tcs.SetResult(0);
                        flushResult = await flushTask;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw ex;
                    }
                });

                await tcs.Task;

                Writer.CancelPendingFlush();

                await task;

                Assert.True(flushResult.IsCanceled);
            }

            // Only half of the data was written because every other flush failed.
            Assert.Equal(16 * 10, ReadWithoutFlush().Length);

            // Start allowing all writes to make read succeed.
            singleWriteStream.AllowAllWrites = true;

            Assert.Equal(16 * 10 * 2, Read().Length);
        }

        [Fact]
        public async Task UseBothStreamAndPipeToWrite()
        {
            await WriteStringToPipeWriter("a");
            WriteStringToStream("c");

            Assert.Equal("ac", ReadAsString());
        }

        [Fact]
        public async Task UsePipeThenStreamToWriteMultipleTimes()
        {
            var expectedString = "abcdef";
            for (var i = 0; i < expectedString.Length; i++)
            {
                if (i % 2 == 0)
                {
                    WriteStringToStream(expectedString[i].ToString());
                }
                else
                {
                    await WriteStringToPipeWriter(expectedString[i].ToString());
                }
            }

            Assert.Equal(expectedString, ReadAsString());
        }

        [Fact]
        public async Task UseStreamThenPipeToWriteMultipleTimes()
        {
            var expectedString = "abcdef";
            for (var i = 0; i < expectedString.Length; i++)
            {
                if (i % 2 == 0)
                {
                    await WriteStringToPipeWriter(expectedString[i].ToString());
                }
                else
                {
                    WriteStringToStream(expectedString[i].ToString());
                }
            }

            Assert.Equal(expectedString, ReadAsString());
        }

        [Fact]
        public void CallCompleteWithoutFlush_ThrowsInvalidOperationException()
        {
            var memory = Writer.GetMemory();
            Writer.Advance(memory.Length);
            var ex = Assert.Throws<InvalidOperationException>(() => Writer.Complete());
            Assert.Equal(ThrowHelper.CreateInvalidOperationException_DataNotAllFlushed().Message, ex.Message);
        }

        [Fact]
        public void CallCompleteWithoutFlushAndException_DoesNotThrowInvalidOperationException()
        {
            var memory = Writer.GetMemory();
            Writer.Advance(memory.Length);
            Writer.Complete(new Exception());
        }

        [Fact]
        public void GetMemorySameAsTheMaxPoolSizeUsesThePool()
        {
            var memory = Writer.GetMemory(Pool.MaxBufferSize);

            Assert.Equal(Pool.MaxBufferSize, memory.Length);
            Assert.Equal(1, Pool.GetRentCount());
        }

        [Fact]
        public void GetMemoryBiggerThanPoolSizeAllocatesUnpooledArray()
        {
            var memory = Writer.GetMemory(Pool.MaxBufferSize + 1);

            Assert.Equal(Pool.MaxBufferSize + 1, memory.Length);
            Assert.Equal(0, Pool.GetRentCount());
        }

        [Fact]
        public void CallComplete_GetMemoryThrows()
        {
            Writer.Complete();
            Assert.Throws<InvalidOperationException>(() => Writer.GetMemory());
        }

        [Fact]
        public void CallComplete_GetSpanThrows()
        {
            Writer.Complete();
            Assert.Throws<InvalidOperationException>(() => Writer.GetSpan());
        }

        [Fact]
        public void DisposeDoesNotThrowIfUnflushedData()
        {
            var streamPipeWriter = new StreamPipeWriter(new MemoryStream());
            streamPipeWriter.Write(new byte[1]);

            streamPipeWriter.Dispose();
        }

        [Fact]
        public void CompleteAfterDisposeDoesNotThrowIfUnflushedData()
        {
            var streamPipeWriter = new StreamPipeWriter(new MemoryStream());
            streamPipeWriter.Write(new byte[1]);

            streamPipeWriter.Dispose();
            streamPipeWriter.Complete();
        }

        [Fact]
        public void CallGetMemoryWithNegativeSizeHint_ThrowsArgException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Writer.GetMemory(-1));
        }

        [Fact]
        public void CallGetSpanWithNegativeSizeHint_ThrowsArgException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Writer.GetSpan(-1));
        }

        [Fact]
        public async Task GetMemorySlicesCorrectly()
        {
            var expectedString = "abcdef";
            var memory = Writer.GetMemory();

            Encoding.ASCII.GetBytes("abc").CopyTo(memory);
            Writer.Advance(3);
            memory = Writer.GetMemory();
            Encoding.ASCII.GetBytes("def").CopyTo(memory);
            Writer.Advance(3);

            await Writer.FlushAsync();
            Assert.Equal(expectedString, ReadAsString());
        }

        [Fact]
        public async Task GetSpanSlicesCorrectly()
        {
            var expectedString = "abcdef";

            void NonAsyncMethod()
            {
                var span = Writer.GetSpan();

                Encoding.ASCII.GetBytes("abc").CopyTo(span);
                Writer.Advance(3);
                span = Writer.GetSpan();
                Encoding.ASCII.GetBytes("def").CopyTo(span);
                Writer.Advance(3);
            }

            NonAsyncMethod();

            await Writer.FlushAsync();
            Assert.Equal(expectedString, ReadAsString());
        }

        [Fact]
        public void InnerStreamReturnsStream()
        {
            Assert.Equal(Stream, ((StreamPipeWriter)Writer).InnerStream);
        }

        private void WriteStringToStream(string input)
        {
            var buffer = Encoding.ASCII.GetBytes(input);
            Stream.Write(buffer, 0, buffer.Length);
        }

        private async Task WriteStringToPipeWriter(string input)
        {
            await Writer.WriteAsync(Encoding.ASCII.GetBytes(input));
        }

        private async Task CheckWriteIsNotCanceled()
        {
            var flushResult = await Writer.WriteAsync(Encoding.ASCII.GetBytes("data"));
            Assert.False(flushResult.IsCanceled);
        }

        private async Task CheckCanceledFlush()
        {
            PipeWriter writableBuffer = Writer.WriteEmpty(MaximumSizeHigh);

            Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();
            Assert.True(flushResult.IsCanceled);
            await writableBuffer.FlushAsync();
        }
    }

    internal class HangingStream : MemoryStream
    {
        public HangingStream()
        {
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
            return 0;
        }

        // Keeping as this code will eventually be ported to corefx
#if NETCOREAPP3_0
        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            await Task.Delay(30000, cancellationToken);
            return 0;
        }
#endif
    }

    internal class SingleWriteStream : MemoryStream
    {
        private bool _shouldNextWriteFail;

        public bool AllowAllWrites { get; set; }

        // Keeping as this code will eventually be ported to corefx
#if NETCOREAPP3_0
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_shouldNextWriteFail && !AllowAllWrites)
                {
                    await Task.Delay(30000, cancellationToken);
                }
                else
                {
                    await base.WriteAsync(source, cancellationToken);
                }
            }
            finally
            {
                _shouldNextWriteFail = !_shouldNextWriteFail;
            }
        }
#endif

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                if (_shouldNextWriteFail && !AllowAllWrites)
                {
                    await Task.Delay(30000, cancellationToken);
                }
                await base.WriteAsync(buffer, offset, count, cancellationToken);
            }
            finally
            {
                _shouldNextWriteFail = !_shouldNextWriteFail;
            }
        }
    }

    internal class CannotFlushStream : MemoryStream
    {
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(30000, cancellationToken);
        }
    }

    internal static class TestWriterExtensions
    {
        public static PipeWriter WriteEmpty(this PipeWriter Writer, int count)
        {
            Writer.GetSpan(count).Slice(0, count).Fill(0);
            Writer.Advance(count);
            return Writer;
        }
    }
}
