// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class WritingAdaptersInteropTests : PipeStreamTest
    {
        [Fact]
        public async Task CheckBasicWriteInterop()
        {
            var pipe = new Pipe();
            var writeOnlyStream = new WriteOnlyPipeStream(pipe.Writer);
            var pipeWriter = new StreamPipeWriter(writeOnlyStream);
            await pipeWriter.WriteAsync(new byte[10]);

            var res = await pipe.Reader.ReadAsync();
            Assert.Equal(new byte[10], res.Buffer.ToArray());
        }

        [Fact]
        public async Task CheckVeryNestedWriteInterop()
        {
            var pipe = new Pipe();
            var writer = pipe.Writer;
            for (var i = 0; i < 3; i++)
            {
                var writeOnlyStream = new WriteOnlyPipeStream(writer);
                writer = new StreamPipeWriter(writeOnlyStream);
            }

            await writer.WriteAsync(new byte[10]);

            var res = await pipe.Reader.ReadAsync();
            Assert.Equal(new byte[10], res.Buffer.ToArray());
        }

        // have to check other internleaved combo

        [Fact]
        public async Task WritesCanBeCanceledViaProvidedCancellationToken()
        {
            var writeOnlyStream = new WriteOnlyPipeStream(new HangingPipeWriter());
            var pipeWriter = new StreamPipeWriter(writeOnlyStream);
            var cts = new CancellationTokenSource(1);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await pipeWriter.WriteAsync(new byte[1], cts.Token));
        }

        [Fact]
        public async Task WriteCanBeCanceledViaCancelPendingFlushWhenFlushIsAsync()
        {
            var writeOnlyStream = new WriteOnlyPipeStream(new HangingPipeWriter());
            var pipeWriter = new StreamPipeWriter(writeOnlyStream);

            FlushResult flushResult = new FlushResult();
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var task = Task.Run(async () =>
            {
                try
                {
                    var writingTask = pipeWriter.WriteAsync(new byte[1]);
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

        private class HangingPipeWriter : PipeWriter
        {
            public override void Advance(int bytes)
            {
            }

            public override void CancelPendingFlush()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                await Task.Delay(30000, cancellationToken);
                return new FlushResult();
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                return new Memory<byte>(new byte[4096]);
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                return new Span<byte>(new byte[4096]);
            }

            public override void OnReaderCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
