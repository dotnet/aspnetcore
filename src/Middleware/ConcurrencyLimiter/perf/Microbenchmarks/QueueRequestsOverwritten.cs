// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.ConcurrencyLimiter.Tests;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ConcurrencyLimiter.Microbenchmarks;

public class QueueRequestsOverwritten
{
    private const int _numRejects = 5000;
    private readonly int _queueLength = 20;
    private int _rejectionCount = 0;
    private readonly ManualResetEventSlim _mres = new ManualResetEventSlim();

#pragma warning disable CS0618 // Type or member is obsolete
    private ConcurrencyLimiterMiddleware _middlewareQueue;
    private ConcurrencyLimiterMiddleware _middlewareStack;
#pragma warning restore CS0618 // Type or member is obsolete

    [GlobalSetup]
    public void GlobalSetup()
    {
        _middlewareQueue = TestUtils.CreateTestMiddleware_QueuePolicy(
            maxConcurrentRequests: 1,
            requestQueueLimit: 20,
            next: WaitForever,
            onRejected: IncrementRejections);

        _middlewareStack = TestUtils.CreateTestMiddleware_StackPolicy(
            maxConcurrentRequests: 1,
            requestQueueLimit: 20,
            next: WaitForever,
            onRejected: IncrementRejections);
    }

    [IterationSetup]
    public void Setup()
    {
        _rejectionCount = 0;
        _mres.Reset();
    }

    private async Task IncrementRejections(HttpContext context)
    {
        if (Interlocked.Increment(ref _rejectionCount) == _numRejects)
        {
            _mres.Set();
        }

        await Task.Yield();
    }

    private async Task WaitForever(HttpContext context)
    {
        await Task.Delay(int.MaxValue);
    }

    [Benchmark(OperationsPerInvoke = _numRejects)]
    public void Baseline()
    {
        var toSend = _queueLength + _numRejects + 1;
        for (int i = 0; i < toSend; i++)
        {
            _ = IncrementRejections(new DefaultHttpContext());
        }

        _mres.Wait();
    }

    [Benchmark(OperationsPerInvoke = _numRejects)]
    public void RejectingRapidly_QueuePolicy()
    {
        var toSend = _queueLength + _numRejects + 1;
        for (int i = 0; i < toSend; i++)
        {
            _ = _middlewareQueue.Invoke(new DefaultHttpContext());
        }

        _mres.Wait();
    }

    [Benchmark(OperationsPerInvoke = _numRejects)]
    public void RejectingRapidly_StackPolicy()
    {
        var toSend = _queueLength + _numRejects + 1;
        for (int i = 0; i < toSend; i++)
        {
            _ = _middlewareStack.Invoke(new DefaultHttpContext());
        }

        _mres.Wait();
    }
}
