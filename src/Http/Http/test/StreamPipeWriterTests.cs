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

namespace Microsoft.AspNetCore.Http.Tests
{
    public class StreamPipeWriterTests : PipeTest
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
        [InlineData(100, 1000)]
        [InlineData(100, 8000)]
        [InlineData(100, 10000)]
        [InlineData(8000, 100)]
        [InlineData(8000, 8000)]
        public async Task CanAdvanceWithPartialConsumptionOfFirstSegment(int firstWriteLength, int secondWriteLength)
        {
            await Writer.WriteAsync(Encoding.ASCII.GetBytes("a"));

            var expectedLength = firstWriteLength + secondWriteLength + 1;

            var memory = Writer.GetMemory(firstWriteLength);
            Writer.Advance(firstWriteLength);

            memory = Writer.GetMemory(secondWriteLength);
            Writer.Advance(secondWriteLength);

            await Writer.FlushAsync();

            Assert.Equal(expectedLength, Read().Length);
        }

        [Fact]
        public async Task ThrowsOnCompleteAndWrite()
        {
            Writer.Complete(new InvalidOperationException("Whoops"));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Writer.FlushAsync());

            Assert.Equal("Whoops", exception.Message);
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
        public void FlushAsyncReturnsCanceledIfCanceledBeforeFlush()
        {
            CheckCanceledFlush();
        }

        [Fact]
        public void FlushAsyncReturnsCanceledIfCanceledBeforeFlushMultipleTimes()
        {
            for (var i = 0; i < 10; i++)
            {
                CheckCanceledFlush();
            }
        }

        [Fact]
        public async Task FlushAsyncReturnsCanceledInterleaved()
        {
            for (var i = 0; i < 5; i++)
            {
                CheckCanceledFlush();
                await CheckWriteIsNotCanceled();
            }
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/4621")]
        public async Task CancelPendingFlushBetweenWritesAllDataIsPreserved()
        {
            MemoryStream = new SingleWriteStream();
            Writer = new StreamPipeWriter(MemoryStream);
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
            MemoryStream = new CannotFlushStream();
            Writer = new StreamPipeWriter(MemoryStream);
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

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/4621")]
        public async Task CancelPendingFlushLostOfCancellationsNoDataLost()
        {
            var writeSize = 16;
            var singleWriteStream = new SingleWriteStream();
            MemoryStream = singleWriteStream;
            Writer = new StreamPipeWriter(MemoryStream, minimumSegmentSize: writeSize);

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

        private async Task CheckWriteIsNotCanceled()
        {
            var flushResult = await Writer.WriteAsync(Encoding.ASCII.GetBytes("data"));
            Assert.False(flushResult.IsCanceled);
        }

        private void CheckCanceledFlush()
        {
            PipeWriter writableBuffer = Writer.WriteEmpty(MaximumSizeHigh);

            Writer.CancelPendingFlush();

            ValueTask<FlushResult> flushAsync = writableBuffer.FlushAsync();

            Assert.True(flushAsync.IsCompleted);
            FlushResult flushResult = flushAsync.GetAwaiter().GetResult();
            Assert.True(flushResult.IsCanceled);
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
#if NETCOREAPP2_2
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


#if NETCOREAPP2_2
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
