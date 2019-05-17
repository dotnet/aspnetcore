// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class SemaphoreWrapperTests
    {
        [Fact]
        public async Task TracksQueueLength()
        {
            using var s = new SemaphoreWrapper(1);
            Assert.Equal(1, s.Count);

            await s.EnterQueue().OrTimeout();
            Assert.Equal(0, s.Count);

            s.LeaveQueue();
            Assert.Equal(1, s.Count);
        }

        [Fact]
        public void DoesNotWaitIfSpaceAvailible()
        {
            using var s = new SemaphoreWrapper(2);

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
            using var s = new SemaphoreWrapper(1);
            await s.EnterQueue().OrTimeout();

            var waitingTask = s.EnterQueue();
            Assert.False(waitingTask.IsCompleted);

            s.LeaveQueue();
            await waitingTask.OrTimeout();
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            using var s1 = new SemaphoreWrapper(1);
            using var s2 = new SemaphoreWrapper(1);

            await s1.EnterQueue().OrTimeout();
            await s2.EnterQueue().OrTimeout();
        }
    }
}
