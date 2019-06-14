// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task RequestsCallNextIfQueueReturnsTrue()
        {
            var flag = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestStrategy.AlwaysPass,
                next: (context) => {
                    flag = true;
                    return Task.CompletedTask;
                });

            await middleware.Invoke(new DefaultHttpContext());
            Assert.True(flag);
        }

        [Fact]
        public async Task RequestRejectsIfQueueReturnsFalse()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestStrategy.AlwaysReject);

            var context = new DefaultHttpContext();
            await middleware.Invoke(context);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        [Fact]
        public async void FullQueueInvokesOnRejected()
        {
            bool onRejectedInvoked = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestStrategy.AlwaysReject,
                onRejected: httpContext =>
                {
                    onRejectedInvoked = true;
                    return Task.CompletedTask;
                });

            var context = new DefaultHttpContext();
            await middleware.Invoke(context).OrTimeout();
            Assert.True(onRejectedInvoked);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        [Fact]
        public async void RequestsBlockedIfQueueFull()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestStrategy.AlwaysReject,
                next: httpContext =>
                {
                    // throttle should bounce the request; it should never get here
                    throw new NotImplementedException();
                });

            await middleware.Invoke(new DefaultHttpContext()).OrTimeout();
        }

        [Fact]
        public void IncomingRequestsFillUpQueue()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestStrategy.AlwaysBlock);

            Assert.Equal(0, middleware.QueuedRequestCount);

            _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, middleware.QueuedRequestCount);

            _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(2, middleware.QueuedRequestCount);
        }

        [Fact]
        public async Task CleanupHappensEvenIfNextErrors()
        {
            var flag = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: new TestStrategy(
                    invoke: (() => true),
                    onExit: () => { flag = true; }),
                next: httpContext =>
                {
                    throw new DivideByZeroException();
                });

            Assert.Equal(0, middleware.QueuedRequestCount);
            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(new DefaultHttpContext())).OrTimeout();

            Assert.Equal(0, middleware.QueuedRequestCount);
            Assert.True(flag);
        }

        [Fact]
        public async void ExceptionThrownDuringOnRejected()
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();

            var middleware = TestUtils.CreateTestMiddleware(
                onRejected: httpContext =>
                {
                    throw new DivideByZeroException();
                },
                next: httpContext =>
                {
                    return tsc.Task;
                });

            var firstRequest = middleware.Invoke(new DefaultHttpContext());

            var context = new DefaultHttpContext();
            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(context)).OrTimeout();
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

            tsc.SetResult(true);

            Assert.True(firstRequest.IsCompletedSuccessfully);

            var thirdRequest = middleware.Invoke(new DefaultHttpContext());

            Assert.True(thirdRequest.IsCompletedSuccessfully);

            Assert.Equal(0, middleware.QueuedRequestCount);
        }
    }
}
