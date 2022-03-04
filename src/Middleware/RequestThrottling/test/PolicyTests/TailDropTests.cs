// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests.PolicyTests
{
    public class TailDropTests
    {
        [Fact]
        public void DoesNotWaitIfSpaceAvailible()
        {
            using var s = TestUtils.CreateTailDropQueue(2);

            var t1 = s.TryEnterAsync();
            Assert.True(t1.IsCompleted);

            var t2 = s.TryEnterAsync();
            Assert.True(t2.IsCompleted);

            var t3 = s.TryEnterAsync();
            Assert.False(t3.IsCompleted);
        }

        [Fact]
        public async Task WaitsIfNoSpaceAvailible()
        {
            using var s = TestUtils.CreateTailDropQueue(1);
            Assert.True(await s.TryEnterAsync().OrTimeout());

            var waitingTask = s.TryEnterAsync();
            Assert.False(waitingTask.IsCompleted);

            s.OnExit();
            Assert.True(await waitingTask.OrTimeout());
        }

        [Fact]
        public async Task IsEncapsulated()
        {
            using var s1 = TestUtils.CreateTailDropQueue(1);
            using var s2 = TestUtils.CreateTailDropQueue(1);

            Assert.True(await s1.TryEnterAsync().OrTimeout());
            Assert.True(await s2.TryEnterAsync().OrTimeout());
        }
    }
}
