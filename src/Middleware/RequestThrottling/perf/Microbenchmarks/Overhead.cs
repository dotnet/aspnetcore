// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Microbenchmarks
{
    public class Overhead
    {
        private const int _numRequests = 2000;

        private RequestThrottlingMiddleware _middleware;
        private RequestDelegate _nextDelay;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _nextDelay = NextDelay;

            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = MaxConcurrentRequests,
                RequestQueueLimit = RequestQueueLimit
            };

            _middleware = new RequestThrottlingMiddleware(
                    next: _nextDelay,
                    loggerFactory: NullLoggerFactory.Instance,
                    options: Options.Create(options)
                );
        }

        [Params(8)]
        public int MaxConcurrentRequests;

        [Params(_numRequests)]
        public int RequestQueueLimit;

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public async Task Baseline()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                await NextDelay(null);
            }
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public async Task WithEmptyQueueOverhead()
        {
            // this one should not change with MaxConcurrentRequests
            for (int i = 0; i < _numRequests; i++)
            {
                await _middleware.Invoke(null);
            }
        }

        private async Task NextDelay(HttpContext context)
        {
            for (int i = 0; i < 1000; i++) { }
            await Task.Yield();
        }
    }
}
