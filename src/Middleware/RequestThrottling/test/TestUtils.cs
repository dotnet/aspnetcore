// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Threading;
using System.Reflection.Metadata.Ecma335;
using System;
using Microsoft.AspNetCore.RequestThrottling.Strategies;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    static class TestUtils
    {
        public static RequestThrottlingMiddleware CreateTestMiddleware(IRequestQueue queue=null, RequestDelegate onRejected=null, RequestDelegate next=null)
        {
            var options = Options.Create(new RequestThrottlingOptions {
                OnRejected = onRejected ?? (context => Task.CompletedTask),
            });

            return new RequestThrottlingMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    queue: queue ?? CreateTailDropQueue(1, 0),
                    options: options
                );
        }

        internal static TailDrop CreateTailDropQueue(int maxConcurrentRequests, int requestQueueLength = 5000)
        {
            var options = Options.Create(new TailDropOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestQueueLength
            });

            return new TailDrop(options);
        }
    }

    public class TestStrategy : IRequestQueue
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
            onExit) { }

        public async Task<bool> TryEnterQueueAsync()
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
