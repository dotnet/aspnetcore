// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Tests
{
    public static class TestUtils
    {
        public static ConcurrencyLimiterMiddleware CreateTestMiddleware(IQueuePolicy queue = null, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            var options = Options.Create(new ConcurrencyLimiterOptions
            {
                OnRejected = onRejected ?? (context => Task.CompletedTask),
            });

            return new ConcurrencyLimiterMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    queue: queue ?? CreateQueuePolicy(1, 0),
                    options: options
                );
        }

        public static ConcurrencyLimiterMiddleware CreateTestMiddleware_QueuePolicy(int maxConcurrentRequests, int requestQueueLimit, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            return CreateTestMiddleware(
                queue: CreateQueuePolicy(maxConcurrentRequests, requestQueueLimit),
                onRejected: onRejected,
                next: next
                );
        }

        public static ConcurrencyLimiterMiddleware CreateTestMiddleware_StackPolicy(int maxConcurrentRequests, int requestQueueLimit, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            return CreateTestMiddleware(
                queue: CreateStackPolicy(maxConcurrentRequests, requestQueueLimit),
                onRejected: onRejected,
                next: next
                );
        }

        internal static StackPolicy CreateStackPolicy(int maxConcurrentRequests, int requestsQueuelimit = 100)
        {
            var options = Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestsQueuelimit
            });

            return new StackPolicy(options);
        }

        internal static QueuePolicy CreateQueuePolicy(int maxConcurrentRequests, int requestQueueLimit = 100)
        {
            var options = Options.Create(new QueuePolicyOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestQueueLimit
            });

            return new QueuePolicy(options);
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
            this(state => Task.FromResult(onTryEnter(state))
            , onExit) { }
 
        public async ValueTask<bool> TryEnterAsync()
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
