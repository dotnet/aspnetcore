// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests.PolicyTests
{
    public class QueuePolicyTests
    {
        [Fact]
        public void DoesNotWaitIfSpaceAvailable()
        {
            using var s = TestUtils.CreateQueuePolicy(2);

            var t1 = s.TryEnterAsync();
            Assert.True(t1.IsCompleted);

            var t2 = s.TryEnterAsync();
            Assert.True(t2.IsCompleted);

            var t3 = s.TryEnterAsync();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task WaitsIfNoSpaceAvailable()
        {
            using var s = TestUtils.CreateQueuePolicy(1);
            Assert.True(await s.TryEnterAsync().DefaultTimeout());

            var waitingTask = s.TryEnterAsync();
            Assert.False(waitingTask.IsCompleted);

            s.OnExit();
            Assert.True(await waitingTask.DefaultTimeout());
        }

        [Fact]
        public void DoesNotWaitIfQueueFull()
        {
            using var s = TestUtils.CreateQueuePolicy(2, 1);

            var t1 = s.TryEnterAsync();
            Assert.True(t1.IsCompleted);
            Assert.True(t1.Result);

            var t2 = s.TryEnterAsync();
            Assert.True(t2.IsCompleted);
            Assert.True(t2.Result);

            var t3 = s.TryEnterAsync();
            Assert.False(t3.IsCompleted);

            var t4 = s.TryEnterAsync();
            Assert.True(t4.IsCompleted);
            Assert.False(t4.Result);
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            using var s1 = TestUtils.CreateQueuePolicy(1);
            using var s2 = TestUtils.CreateQueuePolicy(1);

            Assert.True(await s1.TryEnterAsync().DefaultTimeout());
            Assert.True(await s2.TryEnterAsync().DefaultTimeout());
        }
    }
}
