// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.ConcurrencyLimiter.Tests;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Microbenchmarks;

public class QueueEmptyOverhead
{
    private const int _numRequests = 20000;

#pragma warning disable CS0618 // Type or member is obsolete
    private ConcurrencyLimiterMiddleware _middlewareQueue;
    private ConcurrencyLimiterMiddleware _middlewareStack;
#pragma warning restore CS0618 // Type or member is obsolete
    private RequestDelegate _restOfServer;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _restOfServer = YieldsThreadInternally ? (RequestDelegate)YieldsThread : (RequestDelegate)CompletesImmediately;

        _middlewareQueue = TestUtils.CreateTestMiddleware_QueuePolicy(
            maxConcurrentRequests: 1,
            requestQueueLimit: 100,
            next: _restOfServer);

        _middlewareStack = TestUtils.CreateTestMiddleware_StackPolicy(
            maxConcurrentRequests: 1,
            requestQueueLimit: 100,
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
    public async Task WithEmptyQueueOverhead_QueuePolicy()
    {
        for (int i = 0; i < _numRequests; i++)
        {
            await _middlewareQueue.Invoke(null);
        }
    }

    [Benchmark(OperationsPerInvoke = _numRequests)]
    public async Task WithEmptyQueueOverhead_StackPolicy()
    {
        for (int i = 0; i < _numRequests; i++)
        {
            await _middlewareStack.Invoke(null);
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
