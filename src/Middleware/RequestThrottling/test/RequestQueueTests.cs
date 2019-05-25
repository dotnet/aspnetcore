// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.RequestThrottling.Internal;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class RequestQueueTests
    {
        [Fact]
        public async Task LimitsIncomingRequests()
        {
            using var s = new RequestQueue(maxConcurrentRequests: 2, requestQueueLimit: 999);
            Assert.Equal(1, s.Count);

            await s.EnterQueue().OrTimeout();
            Assert.Equal(0, s.Count);

            s.Release();
            Assert.Equal(1, s.Count);
        }

        [Fact]
        public async Task TracksQueueLength()
        {
            using var s = new RequestQueue(maxConcurrentRequests: 1, requestQueueLimit: 999);
            Assert.Equal(0, s.WaitingRequests);

            await s.EnterQueue();
            Assert.Equal(0, s.WaitingRequests);

            var enterQueueTask = s.EnterQueue();
            Assert.Equal(1, s.WaitingRequests);

            s.Release();
            await enterQueueTask;
            Assert.Equal(0, s.WaitingRequests);
        }

        [Fact]
        public void DoesNotWaitIfSpaceAvailible()
        {
            using var s = new RequestQueue(maxConcurrentRequests: 2, requestQueueLimit: 999);

            var t1 = s.EnterQueue();
            Assert.True(t1.IsCompleted);

            var t2 = s.EnterQueue();
            Assert.True(t2.IsCompleted);

            var t3 = s.EnterQueue();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task WaitsIfNoSpaceAvailible()
        {
            using var s = new RequestQueue(maxConcurrentRequests: 1, requestQueueLimit: 999);
            await s.EnterQueue().OrTimeout();

            var waitingTask = s.EnterQueue();
            Assert.False(waitingTask.IsCompleted);

            s.Release();
            await waitingTask.OrTimeout();
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            using var s1 = new RequestQueue(maxConcurrentRequests: 1, requestQueueLimit: 999);
            using var s2 = new RequestQueue(maxConcurrentRequests: 1, requestQueueLimit: 999);

            await s1.EnterQueue().OrTimeout();
            await s2.EnterQueue().OrTimeout();
        }
    }
}
