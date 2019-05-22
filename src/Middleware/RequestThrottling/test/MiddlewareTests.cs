// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task RequestsCanEnterIfSpaceAvailible()
        {
            var middleware = TestUtils.CreateTestMiddleWare(maxConcurrentRequests: 1);
            var context = new DefaultHttpContext();

            // a request should go through with no problems
            await middleware.Invoke(context).OrTimeout();
        }

        [Fact]
        public async Task SemaphoreStatePreservedIfRequestsError()
        {
            var middleware = TestUtils.CreateTestMiddleWare(
                maxConcurrentRequests: 1,
                next: httpContext =>
                {
                    throw new DivideByZeroException();
                });

            Assert.Equal(0, middleware.ConcurrentRequests);

            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(new DefaultHttpContext()));

            Assert.Equal(0, middleware.ConcurrentRequests);
        }

        [Fact]
        public async Task QueuedRequestsContinueWhenSpaceBecomesAvailible()
        {
            var blocker = new SyncPoint();
            var firstRequest = true;

            var middleware = TestUtils.CreateTestMiddleWare(
                maxConcurrentRequests: 1,
                next: httpContext =>
                {
                    if (firstRequest)
                    {
                        firstRequest = false;
                        return blocker.WaitToContinue();
                    }
                    return Task.CompletedTask;
                });

            // t1 (as the first request) is blocked by the tcs blocker
            var t1 = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, middleware.ConcurrentRequests);
            Assert.Equal(0, middleware.WaitingRequests);

            // t2 is blocked from entering the server since t1 already exists there
            // note: increasing MaxConcurrentRequests would allow t2 through while t1 is blocked
            var t2 = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, middleware.ConcurrentRequests);
            Assert.Equal(1, middleware.WaitingRequests);

            // unblock the first task, and the second should follow
            blocker.Continue();
            await t1.OrTimeout();
            await t2.OrTimeout();
        }
    }
}
