// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.RequestQueue.Tests
{
    public class SemaphoreWrapperTests
    {
        [Fact]
        public async Task TestBehavior()
        {
            var s = new SemaphoreWrapper(1);
            Assert.Equal(1, s.SpotsLeft);

            await s.EnterQueue();
            Assert.Equal(0, s.SpotsLeft);

            s.LeaveQueue();
            Assert.Equal(1, s.SpotsLeft);
        }

        [Fact]
        public void TestQueueLength()
        {
            var s = new SemaphoreWrapper(2);

            var t1 = s.EnterQueue();
            Assert.True(t1.IsCompleted);

            var t2 = s.EnterQueue();
            Assert.True(t2.IsCompleted);

            var t3 = s.EnterQueue();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task TestWaiting()
        {
            var s = new SemaphoreWrapper(1);
            await s.EnterQueue();

            var waitingTask = s.EnterQueue();
            Assert.False(waitingTask.IsCompleted);

            s.LeaveQueue();
            await waitingTask.TimeoutAfter(TimeSpan.FromSeconds(1));
        }
    }
}
