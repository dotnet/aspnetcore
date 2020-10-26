// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.ConcurrencyLimiter.Tests;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Microbenchmarks
{
    public class QueueFullOverhead
    {
        private const int _numRequests = 2000;
        private int _requestCount = 0;
        private ManualResetEventSlim _mres = new ManualResetEventSlim();

        private ConcurrencyLimiterMiddleware _middlewareQueue;
        private ConcurrencyLimiterMiddleware _middlewareStack;

        [Params(8)]
        public int MaxConcurrentRequests;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _middlewareQueue = TestUtils.CreateTestMiddleware_QueuePolicy(
                maxConcurrentRequests: MaxConcurrentRequests,
                requestQueueLimit: _numRequests,
                next: IncrementAndCheck);

            _middlewareStack = TestUtils.CreateTestMiddleware_StackPolicy(
                maxConcurrentRequests: MaxConcurrentRequests,
                requestQueueLimit: _numRequests,
                next: IncrementAndCheck);
        }

        [IterationSetup]
        public void Setup()
        {
            _requestCount = 0;
            _mres.Reset();
        }

        private async Task IncrementAndCheck(HttpContext context)
        {
            if (Interlocked.Increment(ref _requestCount) == _numRequests)
            {
                _mres.Set();
            }

            await Task.Yield();
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public void Baseline()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                _ = IncrementAndCheck(null);
            }

            _mres.Wait();
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public void QueueingAll_QueuePolicy()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                _ = _middlewareStack.Invoke(null);
            }

            _mres.Wait();
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public void QueueingAll_StackPolicy()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                _ = _middlewareQueue.Invoke(null);
            }

            _mres.Wait();
        }
    }
}
