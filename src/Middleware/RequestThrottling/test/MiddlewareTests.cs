// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task RequestsCanEnterIfSpaceAvailible()
        {
            var middleware = TestUtils.CreateTestMiddleware(maxConcurrentRequests: 1);
            var context = new DefaultHttpContext();

            // a request should go through with no problems
            await middleware.Invoke(context).OrTimeout();
        }

        [Fact]
        public async Task SemaphoreStatePreservedIfRequestsError()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                maxConcurrentRequests: 1,
                next: httpContext =>
                {
                    throw new DivideByZeroException();
                });

            Assert.Equal(0, middleware.ActiveRequestCount);

            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(new DefaultHttpContext()));

            Assert.Equal(0, middleware.ActiveRequestCount);
        }

        [Fact]
        public async Task QueuedRequestsContinueWhenSpaceBecomesAvailible()
        {
            var blocker = new SyncPoint();
            var firstRequest = true;

            var middleware = TestUtils.CreateTestMiddleware(
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
            Assert.Equal(1, middleware.ActiveRequestCount);

            // t2 is blocked from entering the server since t1 already exists there
            // note: increasing MaxConcurrentRequests would allow t2 through while t1 is blocked
            var t2 = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(2, middleware.ActiveRequestCount);

            // unblock the first task, and the second should follow
            blocker.Continue();
            await t1.OrTimeout();
            await t2.OrTimeout();
        }

        [Fact]
        public void InvalidArgumentIfMaxConcurrentRequestsIsNull()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                TestUtils.CreateTestMiddleware(maxConcurrentRequests: null);
            });
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public async void RequestsBlockedIfQueueFull()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                maxConcurrentRequests: 0,
                requestQueueLimit: 0,
                next: httpContext =>
                {
                    // throttle should bounce the request; it should never get here
                    throw new NotImplementedException();
                });

            await middleware.Invoke(new DefaultHttpContext());
        }

        [Fact]
        public async void FullQueueResultsIn503Error()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                maxConcurrentRequests: 0,
                requestQueueLimit: 0);

            var context = new DefaultHttpContext();
            await middleware.Invoke(context);
            Assert.Equal(503, context.Response.StatusCode);
        }

        [Fact]
        public void MultipleRequestsFillUpQueue()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                maxConcurrentRequests: 0,
                requestQueueLimit: 10,
                next: httpContext =>
                {
                    return Task.Delay(TimeSpan.FromSeconds(30));
                });

            Assert.Equal(0, middleware.ActiveRequestCount);

            var _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, middleware.ActiveRequestCount);

            _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(2, middleware.ActiveRequestCount);
        }
    }
}
