// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public static class TestUtils
    {
        public static RequestThrottlingMiddleware CreateTestMiddleware(IQueuePolicy queue = null, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            var options = Options.Create(new RequestThrottlingOptions
            {
                OnRejected = onRejected ?? (context => Task.CompletedTask),
            });

            return new RequestThrottlingMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    queue: queue ?? CreateTailDropQueue(1, 0),
                    options: options
                );
        }

        public static RequestThrottlingMiddleware CreateTestMiddleware_TailDrop(int maxConcurrentRequests, int requestQueueLimit, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            return CreateTestMiddleware(
                queue: CreateTailDropQueue(maxConcurrentRequests, requestQueueLimit),
                onRejected: onRejected,
                next: next
                );
        }

        public static RequestThrottlingMiddleware CreateTestMiddleware_StackPolicy(int maxConcurrentRequests, int requestQueueLimit, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            return CreateTestMiddleware(
                queue: CreateStackPolicy(maxConcurrentRequests, requestQueueLimit),
                onRejected: onRejected,
                next: next
                );
        }

        internal static StackQueuePolicy CreateStackPolicy(int maxConcurrentRequests, int requestsQueuelimit = 100)
        {
            var options = Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestsQueuelimit
            });

            return new StackQueuePolicy(options);
        }

        internal static TailDropQueuePolicy CreateTailDropQueue(int maxConcurrentRequests, int requestQueueLimit = 100)
        {
            var options = Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestQueueLimit
            });

            return new TailDropQueuePolicy(options);
        }
    }

    internal class TestQueue : IQueuePolicy
    {
        private Func<TestQueue, Task<bool>> _onTryEnter { get; }
        private Action _onExit { get; }

        private int _queuedRequests;
        public int QueuedRequests { get => _queuedRequests; }

        public TestQueue(Func<TestQueue, Task<bool>> onTryEnter, Action onExit = null)
        {
            _onTryEnter = onTryEnter;
            _onExit = onExit ?? (() => { });
        }

        public TestQueue(Func<TestQueue, bool> onTryEnter, Action onExit = null) :
            this(async (state) =>
           {
               await Task.CompletedTask;
               return onTryEnter(state);
           }, onExit) { }
 
        public async Task<bool> TryEnterAsync()
        {
            Interlocked.Increment(ref _queuedRequests);
            var result = await _onTryEnter(this);
            Interlocked.Decrement(ref _queuedRequests);
            return result;
        }

        public void OnExit()
        {
            _onExit();
        }

        public static TestQueue AlwaysFalse =
            new TestQueue((_) => false);

        public static TestQueue AlwaysTrue =
            new TestQueue((_) => true);

        public static TestQueue AlwaysBlock =
            new TestQueue(async (_) =>
            {
                await new SemaphoreSlim(0).WaitAsync();
                return false;
            });
    }
}
