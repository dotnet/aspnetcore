// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadingAdaptersInteropTests
    {
        [Fact]
        public async Task CheckBasicReadPipeApi()
        {
            var pipe = new Pipe();
            var readStream = new ReadOnlyPipeStream(pipe.Reader);
            var pipeReader = new StreamPipeReader(readStream);

            await pipe.Writer.WriteAsync(new byte[10]);
            var res = await pipeReader.ReadAsync();
            Assert.Equal(new byte[10], res.Buffer.ToArray());
        }

        [Fact]
        public async Task CheckNestedPipeApi()
        {
            var pipe = new Pipe();
            var reader = pipe.Reader;
            for (var i = 0; i < 3; i++)
            {
                var readStream = new ReadOnlyPipeStream(reader);
                reader = new StreamPipeReader(readStream);
            }

            await pipe.Writer.WriteAsync(new byte[10]);
            var res = await reader.ReadAsync();
            Assert.Equal(new byte[10], res.Buffer.ToArray());
        }

        [Fact]
        public async Task CheckBasicReadStreamApi()
        {
            var stream = new MemoryStream();
            await stream.WriteAsync(new byte[10]);
            stream.Position = 0;

            var pipeReader = new StreamPipeReader(stream);
            var readOnlyStream = new ReadOnlyPipeStream(pipeReader);

            var resSize = await readOnlyStream.ReadAsync(new byte[10]);

            Assert.Equal(10, resSize);
        }

        [Fact]
        public async Task CheckNestedStreamApi()
        {
            var stream = new MemoryStream();
            await stream.WriteAsync(new byte[10]);
            stream.Position = 0;

            Stream readOnlyStream = stream;
            for (var i = 0; i < 3; i++)
            {
                var pipeReader = new StreamPipeReader(readOnlyStream);
                readOnlyStream = new ReadOnlyPipeStream(pipeReader);
            }

            var resSize = await readOnlyStream.ReadAsync(new byte[10]);

            Assert.Equal(10, resSize);
        }

        [Fact]
        public async Task ReadsCanBeCanceledViaProvidedCancellationToken()
        {
            var readOnlyStream = new ReadOnlyPipeStream(new HangingPipeReader());
            var pipeReader = new StreamPipeReader(readOnlyStream);

            var cts = new CancellationTokenSource(1);
            await Task.Delay(1);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await pipeReader.ReadAsync(cts.Token));
        }

        [Fact]
        public async Task ReadCanBeCancelledViaCancelPendingReadWhenReadIsAsync()
        {
            var readOnlyStream = new ReadOnlyPipeStream(new HangingPipeReader());
            var pipeReader = new StreamPipeReader(readOnlyStream);

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

        private class HangingPipeReader : PipeReader
        {
            public override void AdvanceTo(SequencePosition consumed)
            {
                throw new NotImplementedException();
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override void OnWriterCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                await Task.Delay(30000, cancellationToken);
                return new ReadResult();
            }

            public override bool TryRead(out ReadResult result)
            {
                result = new ReadResult();
                return false;
            }
        }
    }
}
