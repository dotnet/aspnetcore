// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ConcurrentPipeWriterTests
    {
        [Fact]
        public async Task IfFlushIsCalledAgainBeforeTheLastFlushCompletedItWaitsForTheLastCall()
        {
            var mockPipeWriter = new Mock<PipeWriter>();
            var pipeWriterFlushTcsArray = new[] {
                new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                new TaskCompletionSource<FlushResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            };
            var pipeWriterFlushCallCount = 0;

            mockPipeWriter.Setup(p => p.FlushAsync(CancellationToken.None)).Returns(() =>
            {
                return new ValueTask<FlushResult>(pipeWriterFlushTcsArray[pipeWriterFlushCallCount++].Task);
            });

            using (var slabPool = new SlabMemoryPool())
            using (var diagnosticPool = new DiagnosticMemoryPool(slabPool))
            {

                var timingPipeFlusher = new ConcurrentPipeWriter(mockPipeWriter.Object, diagnosticPool);

                var flushTask0 = timingPipeFlusher.FlushAsync();
                var flushTask1 = timingPipeFlusher.FlushAsync();
                var flushTask2 = timingPipeFlusher.FlushAsync();

                Assert.False(flushTask0.IsCompleted);
                Assert.False(flushTask1.IsCompleted);
                Assert.False(flushTask2.IsCompleted);
                Assert.Equal(1, pipeWriterFlushCallCount);

                pipeWriterFlushTcsArray[0].SetResult(default);
                await flushTask0.AsTask().DefaultTimeout();

                //Assert.True(flushTask0.IsCompleted);
                //Assert.False(flushTask1.IsCompleted);
                //Assert.False(flushTask2.IsCompleted);
                //Assert.True(pipeWriterFlushCallCount <= 2);

                //pipeWriterFlushTcsArray[1].SetResult(default);
                //await flushTask1.AsTask().DefaultTimeout();

                //Assert.True(flushTask0.IsCompleted);
                //Assert.True(flushTask1.IsCompleted);
                //Assert.False(flushTask2.IsCompleted);
                //Assert.True(pipeWriterFlushCallCount <= 3);

                //pipeWriterFlushTcsArray[2].SetResult(default);
                //await flushTask2.AsTask().DefaultTimeout();

                //Assert.True(flushTask0.IsCompleted);
                //Assert.True(flushTask1.IsCompleted);
                //Assert.True(flushTask2.IsCompleted);
                //Assert.Equal(3, pipeWriterFlushCallCount);
            }
        }
    }
}
