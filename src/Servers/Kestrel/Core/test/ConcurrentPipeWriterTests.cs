// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ConcurrentPipeWriterTests
    {
        [Fact]
        public async Task IfFlushIsCalledAgainBeforeTheLastFlushCompletedItWaitsForTheLastCall()
        {
            using (var slabPool = new SlabMemoryPool())
            using (var diagnosticPool = new DiagnosticMemoryPool(slabPool))
            {
                var pipeWriterFlushTcsArray = new[] {
                    new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                    new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                };

                var mockPipeWriter = new MockPipeWriter(pipeWriterFlushTcsArray);
                var concurrentPipeWriter = new ConcurrentPipeWriter(mockPipeWriter, diagnosticPool);

                var flushTask0 = concurrentPipeWriter.FlushAsync();

                concurrentPipeWriter.GetMemory();
                concurrentPipeWriter.Advance(1);

                var flushTask1 = concurrentPipeWriter.FlushAsync();

                Assert.False(flushTask0.IsCompleted);
                Assert.False(flushTask1.IsCompleted);
                Assert.Equal(1, mockPipeWriter.FlushCallCount);

                pipeWriterFlushTcsArray[0].SetResult(default);

                Assert.False(flushTask0.IsCompleted);
                Assert.False(flushTask1.IsCompleted);
                Assert.True(mockPipeWriter.FlushCallCount <= 2);

                pipeWriterFlushTcsArray[1].SetResult(default);
                await flushTask0.AsTask().DefaultTimeout();
                await flushTask1.AsTask().DefaultTimeout();

                Assert.Equal(2, mockPipeWriter.FlushCallCount);
            }
        }

        private class MockPipeWriter : PipeWriter
        {
            private readonly TaskCompletionSource<FlushResult>[] _flushResults;

            public MockPipeWriter(TaskCompletionSource<FlushResult>[] flushResults)
            {
                _flushResults = flushResults;
            }

            public int FlushCallCount { get; set; }

            public override void Advance(int bytes)
            {
            }

            public override void CancelPendingFlush()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                return new ValueTask<FlushResult>(_flushResults[FlushCallCount++].Task);
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                return new Memory<byte>(new byte[sizeHint]);
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                return GetMemory(sizeHint).Span;
            }

            public override void OnReaderCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
