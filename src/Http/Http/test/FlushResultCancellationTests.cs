// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class FlushResultCancellationTests : PipeTest
    {
        [Fact]
        public void FlushAsyncCancellationDeadlock()
        {
            var cts = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();

            PipeWriter buffer = Writer.WriteEmpty(MaximumSizeHigh);

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            ValueTaskAwaiter<FlushResult> awaiter = buffer.FlushAsync(cts.Token).GetAwaiter();
            awaiter.OnCompleted(
                async () => {
                    // We are on cancellation thread and need to wait until another FlushAsync call
                    // takes pipe state lock
                    await tcs.Task;

                    // Make sure we had enough time to reach _cancellationTokenRegistration.Dispose
                    Thread.Sleep(100);

                    // Try to take pipe state lock
                    await buffer.FlushAsync();
                });

            // Start a thread that would run cancellation callbacks
            Task cancellationTask = Task.Run(() => cts.Cancel());
            // Start a thread that would call FlushAsync with different token
            // and block on _cancellationTokenRegistration.Dispose
            Task blockingTask = Task.Run(
                () => {
                    tcs.SetResult(0);
                    buffer.FlushAsync(cts2.Token);
                });

            bool completed = Task.WhenAll(cancellationTask, blockingTask).Wait(TimeSpan.FromSeconds(10));
            Assert.True(completed);
        }

        [Fact]
        public async Task FlushAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            var cancellationTokenSource1 = new CancellationTokenSource();
            PipeWriter buffer = Writer.WriteEmpty(10);
            await buffer.FlushAsync(cancellationTokenSource1.Token);

            cancellationTokenSource1.Cancel();

            var cancellationTokenSource2 = new CancellationTokenSource();
            buffer = Writer.WriteEmpty(10);

            await buffer.FlushAsync(cancellationTokenSource2.Token);
        }
    }
}
