// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
                queue: TestPolicy.AlwaysPass,
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
                queue: TestPolicy.AlwaysReject);

            var context = new DefaultHttpContext();
            await middleware.Invoke(context);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        [Fact]
        public async void FullQueueInvokesOnRejected()
        {
            bool onRejectedInvoked = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestPolicy.AlwaysReject,
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
                queue: TestPolicy.AlwaysReject,
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
            var testQueue = TestPolicy.AlwaysBlock;
            var middleware = TestUtils.CreateTestMiddleware(testQueue);

            Assert.Equal(0, testQueue.QueuedRequests);

            _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, testQueue.QueuedRequests);

            _ = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(2, testQueue.QueuedRequests);
        }

        [Fact]
        public void EventCountersTrackQueuedRequests()
        {
            var blocker = new TaskCompletionSource<bool>();

            // requires
            var testQueue = new TestPolicy(
                invoke: async (_) =>
                {
                    return await blocker.Task;
                });
            var middleware = TestUtils.CreateTestMiddleware(testQueue);

            Assert.Equal(0, testQueue.QueuedRequests);

            var task1 = middleware.Invoke(new DefaultHttpContext());
            Assert.False(task1.IsCompleted);
            Assert.Equal(1, testQueue.QueuedRequests);

            blocker.SetResult(true);

            Assert.Equal(0, testQueue.QueuedRequests);
        }

        [Fact]
        public async Task CleanupHappensEvenIfNextErrors()
        {
            var flag = false;

            var testQueue = new TestPolicy(
                    invoke: (_) => true,
                    onExit: () => { flag = true; });

            var middleware = TestUtils.CreateTestMiddleware(
                queue: testQueue,
                next: httpContext =>
                {
                    throw new DivideByZeroException();
                });

            Assert.Equal(0, testQueue.QueuedRequests);
            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(new DefaultHttpContext())).OrTimeout();

            Assert.Equal(0, testQueue.QueuedRequests);
            Assert.True(flag);
        }

        [Fact]
        public async void ExceptionThrownDuringOnRejected()
        {
            TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();

            var testQueue = new TestPolicy(
                invoke: (state) =>
                {
                    return state.QueuedRequests == 0;
                });

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

            Assert.Equal(0, testQueue.QueuedRequests);
        }
    }
}
