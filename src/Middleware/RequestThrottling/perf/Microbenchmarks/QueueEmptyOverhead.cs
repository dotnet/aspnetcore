// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling.Tests;

namespace Microsoft.AspNetCore.RequestThrottling.Microbenchmarks
{
    public class QueueEmptyOverhead
    {
        private const int _numRequests = 20000;

        private RequestThrottlingMiddleware _middleware;
        private RequestDelegate _restOfServer;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _restOfServer = YieldsThreadInternally ? (RequestDelegate)YieldsThread : (RequestDelegate)CompletesImmediately;

            _middleware = TestUtils.CreateTestMiddleware_TailDrop(
                maxConcurrentRequests: 8,
                requestQueueLimit: 2000,
                next: _restOfServer);
        }

        [Params(false, true)]
        public bool YieldsThreadInternally;

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public async Task Baseline()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                await _restOfServer(null);
            }
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public async Task WithEmptyQueueOverhead()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                await _middleware.Invoke(null);
            }
        }

        private static async Task YieldsThread(HttpContext context)
        {
            await Task.Yield();
        }

        private static Task CompletesImmediately(HttpContext context)
        {
            return Task.CompletedTask;
        }
    }
}
