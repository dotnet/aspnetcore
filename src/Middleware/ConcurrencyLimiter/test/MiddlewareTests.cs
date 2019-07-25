// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests
{
    public class MiddlewareTests
    {
        [Fact]
        public async Task RequestsCallNextIfQueueReturnsTrue()
        {
            var flag = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestQueue.AlwaysTrue,
                next: httpContext =>
                {
                    flag = true;
                    return Task.CompletedTask;
                });

            await middleware.Invoke(new DefaultHttpContext());
            Assert.True(flag);
        }

        [Fact]
        public async Task RequestRejectsIfQueueReturnsFalse()
        {
            bool onRejectedInvoked = false;

            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestQueue.AlwaysFalse,
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
        public async Task RequestsDoesNotEnterIfQueueFull()
        {
            var middleware = TestUtils.CreateTestMiddleware(
                queue: TestQueue.AlwaysFalse,
                next: httpContext =>
                {
                    // throttle should bounce the request; it should never get here
                    throw new DivideByZeroException();
                });

            await middleware.Invoke(new DefaultHttpContext()).OrTimeout();
        }

        [Fact]
        public void IncomingRequestsFillUpQueue()
        {
            var testQueue = TestQueue.AlwaysBlock;
            var middleware = TestUtils.CreateTestMiddleware(testQueue);

            Assert.Equal(0, testQueue.QueuedRequests);

            var task1 = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, testQueue.QueuedRequests);
            Assert.False(task1.IsCompleted);

            var task2 = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(2, testQueue.QueuedRequests);
            Assert.False(task2.IsCompleted);
        }

        [Fact]
        public void EventCountersTrackQueuedRequests()
        {
            var blocker = new TaskCompletionSource<bool>();

            var testQueue = new TestQueue(
                onTryEnter: async (_) =>
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
        public async Task QueueOnExitCalledEvenIfNextErrors()
        {
            var flag = false;

            var testQueue = new TestQueue(
                    onTryEnter: (_) => true,
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
        public async Task ExceptionThrownDuringOnRejected()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            var concurrent = 0;
            var testQueue = new TestQueue(
                onTryEnter: (testQueue) =>
                {
                    if (concurrent > 0)
                    {
                        return false;
                    }
                    else
                    {
                        concurrent++;
                        return true;
                    }
                },
                onExit: () => { concurrent--; });

            var middleware = TestUtils.CreateTestMiddleware(
                queue: testQueue,
                onRejected: httpContext =>
                {
                    throw new DivideByZeroException();
                },
                next: httpContext =>
                {
                    return tcs.Task;
                });

            // the first request enters the server, and is blocked by the tcs
            var firstRequest = middleware.Invoke(new DefaultHttpContext());
            Assert.Equal(1, concurrent);
            Assert.Equal(0, testQueue.QueuedRequests);

            // the second request is rejected with a 503 error. During the rejection, an error occurs
            var context = new DefaultHttpContext();
            await Assert.ThrowsAsync<DivideByZeroException>(() => middleware.Invoke(context)).OrTimeout();
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
            Assert.Equal(1, concurrent);
            Assert.Equal(0, testQueue.QueuedRequests);

            // the first request is unblocked, and the queue continues functioning as expected
            tcs.SetResult(true);
            Assert.True(firstRequest.IsCompletedSuccessfully);
            Assert.Equal(0, concurrent);
            Assert.Equal(0, testQueue.QueuedRequests);

            var thirdRequest = middleware.Invoke(new DefaultHttpContext());
            Assert.True(thirdRequest.IsCompletedSuccessfully);
            Assert.Equal(0, concurrent);
            Assert.Equal(0, testQueue.QueuedRequests);
        }

        [Fact]
        public async Task MiddlewareOnlyCallsGetResultOnce()
        {
            var flag = false;

            var queue = new TestQueueForResettableBoolean();
            var middleware = TestUtils.CreateTestMiddleware(
                queue,
                next: async context =>
                {
                    await Task.CompletedTask;
                    flag = true;
                });

            queue.Source.Complete(true);
            await middleware.Invoke(new DefaultHttpContext());

            Assert.True(flag);
        }

        private class TestQueueForResettableBoolean : IQueuePolicy
        {
            public ResettableBooleanCompletionSource Source;
            public TestQueueForResettableBoolean()
            {
                Source = new ResettableBooleanCompletionSource(TestUtils.CreateStackPolicy(1));
            }

            public ValueTask<bool> TryEnterAsync()
            {
                return Source.GetValueTask();
            }

            public void OnExit() { }
        }
    }
}
