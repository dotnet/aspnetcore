// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class ReadAsyncCancellationTests : PipeTest
    {
        [Fact]
        public async Task AdvanceShouldResetStateIfReadCanceled()
        {
            Reader.CancelPendingRead();

            var result = await Reader.ReadAsync();
            var buffer = result.Buffer;
            Reader.AdvanceTo(buffer.End);

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            Assert.True(buffer.IsEmpty);
        }

        [Fact]
        public async Task CancellingBeforeAdvance()
        {
            Write(Encoding.ASCII.GetBytes("Hello World"));

            var result = await Reader.ReadAsync();
            var buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.False(result.IsCanceled);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));

            Reader.CancelPendingRead();

            Reader.AdvanceTo(buffer.End);

            var awaitable = Reader.ReadAsync();

            Assert.True(awaitable.IsCompleted);

            result = await awaitable;

            Assert.True(result.IsCanceled);

            Reader.AdvanceTo(result.Buffer.Start, result.Buffer.Start);
        }

        [Fact]
        public async Task ReadAsyncWithNewCancellationTokenNotAffectedByPrevious()
        {
            Write(new byte[1]);

            var cancellationTokenSource1 = new CancellationTokenSource();
            var result = await Reader.ReadAsync(cancellationTokenSource1.Token);
            Reader.AdvanceTo(result.Buffer.Start);

            cancellationTokenSource1.Cancel();
            var cancellationTokenSource2 = new CancellationTokenSource();

            // Verifying that ReadAsync does not throw
            result = await Reader.ReadAsync(cancellationTokenSource2.Token);
            Reader.AdvanceTo(result.Buffer.Start);
        }

        [Fact]
        public async Task CancellingPendingReadBeforeReadAsync()
        {
            Reader.CancelPendingRead();

            ReadResult result = await Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;
            Reader.AdvanceTo(buffer.End);

            Assert.False(result.IsCompleted);
            Assert.True(result.IsCanceled);
            Assert.True(buffer.IsEmpty);

            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            Write(bytes);

            result = await Reader.ReadAsync();
            buffer = result.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.False(result.IsCanceled);
            Assert.True(buffer.IsSingleSegment);
            var array = new byte[11];
            buffer.First.Span.CopyTo(array);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(array));

            Reader.AdvanceTo(buffer.Start, buffer.Start);
        }

        [Fact]
        public void ReadAsyncCompletedAfterPreCancellation()
        {
            Reader.CancelPendingRead();
            Write(new byte[] { 1, 2, 3 });

            ValueTaskAwaiter<ReadResult> awaitable = Reader.ReadAsync().GetAwaiter();

            Assert.True(awaitable.IsCompleted);

            ReadResult result = awaitable.GetResult();

            Assert.True(result.IsCanceled);

            awaitable = Reader.ReadAsync().GetAwaiter();

            Assert.True(awaitable.IsCompleted);

            Reader.AdvanceTo(awaitable.GetResult().Buffer.End);
        }
    }
}
