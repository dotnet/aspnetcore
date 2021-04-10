// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TaskExtensionsTest
    {
        [Fact]
        public async Task TimeoutAfterTest()
        {
            var cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).TimeoutAfter(TimeSpan.FromMilliseconds(50)));
            cts.Cancel();
        }

        [Fact]
        public async Task TimeoutAfter_DoesNotThrowWhenCompleted()
        {
            await Task.FromResult(true).TimeoutAfter(TimeSpan.FromMilliseconds(50));
        }

        [Fact]
        public async Task TimeoutAfter_DoesNotThrow_WithinTimeoutPeriod()
        {
            await Task.Delay(10).TimeoutAfter(TimeSpan.FromMilliseconds(50));
        }

        [Fact]
        public async Task DefaultTimeout_WithTimespan()
        {
            var cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(TimeSpan.FromMilliseconds(50)));
            cts.Cancel();
        }

        [Fact]
        public async Task DefaultTimeout_WithMilliseconds()
        {
            var cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(50));
            cts.Cancel();
        }

        [Fact]
        public async Task DefaultTimeout_Message_ContainsLineNumber()
        {
            var cts = new CancellationTokenSource();
            await Assert.ThrowsAsync<TimeoutException>(async () => await Task.Delay(30000, cts.Token).DefaultTimeout(50));
            cts.Cancel();
        }

        [Fact]
        public async Task DefaultTimeout_DoesNotThrowWhenCompleted()
        {
            await Task.FromResult(true).DefaultTimeout();
        }

    }
}
