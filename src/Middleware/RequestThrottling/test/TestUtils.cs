// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling.QueuePolicies;
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

    public class TestStrategy : IQueuePolicy
    {
        private Func<Task<bool>> _invoke { get; }
        private Action _onExit { get; }

        public TestStrategy(Func<Task<bool>> invoke, Action onExit = null)
        {
            _invoke = invoke;
            _onExit = onExit ?? (() => { });
        }

        public TestStrategy(Func<bool> invoke, Action onExit = null)
            : this(async () =>
            {
                await Task.CompletedTask;
                return invoke();
            },
            onExit)
        { }

        public async Task<bool> TryEnterAsync()
        {
            await Task.CompletedTask;
            return await _invoke();
        }

        public void OnExit()
        {
            _onExit();
        }

        public static TestStrategy AlwaysReject =
            new TestStrategy(() => false);

        public static TestStrategy AlwaysPass =
            new TestStrategy(() => true);

        public static TestStrategy AlwaysBlock =
            new TestStrategy(async () =>
            {
                await new SemaphoreSlim(0).WaitAsync();
                return false;
            });
    }
}
