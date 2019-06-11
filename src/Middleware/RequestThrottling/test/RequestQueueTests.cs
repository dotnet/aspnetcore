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
        public void DoesNotWaitIfSpaceAvailible()
        {
            using var s = TestUtils.CreateRequestQueue(2);

            var t1 = s.TryEnterQueueAsync();
            Assert.True(t1.IsCompleted);

            var t2 = s.TryEnterQueueAsync();
            Assert.True(t2.IsCompleted);

            var t3 = s.TryEnterQueueAsync();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task WaitsIfNoSpaceAvailible()
        {
            using var s = TestUtils.CreateRequestQueue(1);
            Assert.True(await s.TryEnterQueueAsync().OrTimeout());

            var waitingTask = s.TryEnterQueueAsync();
            Assert.False(waitingTask.IsCompleted);

            s.OnExit();
            Assert.True(await waitingTask.OrTimeout());
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            using var s1 = TestUtils.CreateRequestQueue(1);
            using var s2 = TestUtils.CreateRequestQueue(1);

            Assert.True(await s1.TryEnterQueueAsync().OrTimeout());
            Assert.True(await s2.TryEnterQueueAsync().OrTimeout());
        }
    }
}
