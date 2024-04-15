// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class StampedeTests
{
    static IDisposable GetDefaultCache(out DefaultHybridCache cache)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDistributedCache, InvalidCache>();
        services.AddSingleton<IMemoryCache, InvalidCache>();
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new()
            {
                Flags = HybridCacheEntryFlags.DisableDistributedCache | HybridCacheEntryFlags.DisableLocalCache
            };
        });
        var provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    public class InvalidCache : IDistributedCache, IMemoryCache
    {
        void IDisposable.Dispose() { }
        ICacheEntry IMemoryCache.CreateEntry(object key) => throw new NotSupportedException("Intentionally not provided");

        byte[]? IDistributedCache.Get(string key) => throw new NotSupportedException("Intentionally not provided");

        Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token) => throw new NotSupportedException("Intentionally not provided");

        void IDistributedCache.Refresh(string key) => throw new NotSupportedException("Intentionally not provided");

        Task IDistributedCache.RefreshAsync(string key, CancellationToken token) => throw new NotSupportedException("Intentionally not provided");

        void IDistributedCache.Remove(string key) => throw new NotSupportedException("Intentionally not provided");

        void IMemoryCache.Remove(object key) => throw new NotSupportedException("Intentionally not provided");

        Task IDistributedCache.RemoveAsync(string key, CancellationToken token) => throw new NotSupportedException("Intentionally not provided");

        void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new NotSupportedException("Intentionally not provided");

        Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token) => throw new NotSupportedException("Intentionally not provided");

        bool IMemoryCache.TryGetValue(object key, out object? value) => throw new NotSupportedException("Intentionally not provided");
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(10, false)]
    [InlineData(10, true)]
    public async Task MultipleCallsShareExecution_NoCancellation(int callerCount, bool canBeCanceled)
    {
        using var scope = GetDefaultCache(out var cache);
        using var semaphore = new SemaphoreSlim(0);

        var token = canBeCanceled ? new CancellationTokenSource().Token : CancellationToken.None;

        int executeCount = 0, cancelCount = 0;
        Task<Guid>[] results = new Task<Guid>[callerCount];
        for (int i = 0; i < callerCount; i++)
        {
            results[i] = cache.GetOrCreateAsync(Me(), async ct =>
            {
                using var reg = ct.Register(() => Interlocked.Increment(ref cancelCount));
                if (!await semaphore.WaitAsync(5_000))
                {
                    throw new TimeoutException("Failed to activate");
                }
                Interlocked.Increment(ref executeCount);
                ct.ThrowIfCancellationRequested(); // assert not cancelled
                return Guid.NewGuid();
            }, token: token).AsTask();
        }

        Assert.Equal(callerCount, cache.DebugGetCallerCount(Me()));

        // everyone is queued up; release the hounds and check
        // that we all got the same result
        Assert.Equal(0, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));
        semaphore.Release();
        var first = await results[0];
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));
        foreach (var result in results)
        {
            Assert.Equal(first, await result);
        }
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));

        // and do it a second time; we expect different results
        Volatile.Write(ref executeCount, 0);
        for (int i = 0; i < callerCount; i++)
        {
            results[i] = cache.GetOrCreateAsync(Me(), async ct =>
            {
                using var reg = ct.Register(() => Interlocked.Increment(ref cancelCount));
                if (!await semaphore.WaitAsync(5_000))
                {
                    throw new TimeoutException("Failed to activate");
                }
                Interlocked.Increment(ref executeCount);
                ct.ThrowIfCancellationRequested(); // assert not cancelled
                return Guid.NewGuid();
            }, token: token).AsTask();
        }

        Assert.Equal(callerCount, cache.DebugGetCallerCount(Me()));

        // everyone is queued up; release the hounds and check
        // that we all got the same result
        Assert.Equal(0, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));
        semaphore.Release();
        var second = await results[0];
        Assert.NotEqual(first, second);
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));
        foreach (var result in results)
        {
            Assert.Equal(second, await result);
        }
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task MultipleCallsShareExecution_EveryoneCancels(int callerCount)
    {
        // what we want to prove here is that everyone ends up cancelling promptly by
        // *their own* cancellation (not dependent on the shared task), and that
        // the shared task becomes cancelled (which can be later)

        using var scope = GetDefaultCache(out var cache);
        using var semaphore = new SemaphoreSlim(0);

        int executeCount = 0, cancelCount = 0;
        Task<Guid>[] results = new Task<Guid>[callerCount];
        CancellationTokenSource[] cancels = new CancellationTokenSource[callerCount];
        for (int i = 0; i < callerCount; i++)
        {
            cancels[i] = new CancellationTokenSource();
            results[i] = cache.GetOrCreateAsync(Me(), async ct =>
            {
                using var reg = ct.Register(() => Interlocked.Increment(ref cancelCount));
                if (!await semaphore.WaitAsync(5_000))
                {
                    throw new TimeoutException("Failed to activate");
                }
                try
                {
                    Interlocked.Increment(ref executeCount);
                    ct.ThrowIfCancellationRequested();
                    return Guid.NewGuid();
                }
                finally
                {
                    semaphore.Release(); // handshake so we can check when available again
                }
            }, token: cancels[i].Token).AsTask();
        }

        Assert.Equal(callerCount, cache.DebugGetCallerCount(Me()));

        // everyone is queued up; release the hounds and check
        // that we all got the same result
        foreach (var cancel in cancels)
        {
            cancel.Cancel();
        }
        await Task.Delay(500); // cancellation happens on a worker; need to allow a moment
        for (int i = 0; i < callerCount; i++)
        {
            var result = results[i];
            // should have already cancelled, even though underlying task hasn't finished yet
            Assert.Equal(TaskStatus.Canceled, result.Status);
            var ex = Assert.Throws<OperationCanceledException>(() => result.GetAwaiter().GetResult());
            Assert.Equal(cancels[i].Token, ex.CancellationToken); // each gets the correct blame
        }

        Assert.Equal(0, Volatile.Read(ref executeCount));
        semaphore.Release();
        if (!await semaphore.WaitAsync(5_000)) // wait for underlying task to hand back to us
        {
            throw new TimeoutException("Didn't get handshake back from task");
        }
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(1, Volatile.Read(ref cancelCount));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(10, 0)]
    [InlineData(10, 1)]
    [InlineData(10, 7)]
    public async Task MultipleCallsShareExecution_MostCancel(int callerCount, int remaining)
    {
        Assert.True(callerCount >= 2); // "most" is not "one"

        // what we want to prove here is that everyone ends up cancelling promptly by
        // *their own* cancellation (not dependent on the shared task), and that
        // the shared task becomes cancelled (which can be later)

        using var scope = GetDefaultCache(out var cache);
        using var semaphore = new SemaphoreSlim(0);

        int executeCount = 0, cancelCount = 0;
        Task<Guid>[] results = new Task<Guid>[callerCount];
        CancellationTokenSource[] cancels = new CancellationTokenSource[callerCount];
        for (int i = 0; i < callerCount; i++)
        {
            cancels[i] = new CancellationTokenSource();
            results[i] = cache.GetOrCreateAsync(Me(), async ct =>
            {
                using var reg = ct.Register(() => Interlocked.Increment(ref cancelCount));
                if (!await semaphore.WaitAsync(5_000))
                {
                    throw new TimeoutException("Failed to activate");
                }
                try
                {
                    Interlocked.Increment(ref executeCount);
                    ct.ThrowIfCancellationRequested();
                    return Guid.NewGuid();
                }
                finally
                {
                    semaphore.Release(); // handshake so we can check when available again
                }
            }, token: cancels[i].Token).AsTask();
        }

        Assert.Equal(callerCount, cache.DebugGetCallerCount(Me()));

        // everyone is queued up; release the hounds and check
        // that we all got the same result
        for (int i = 0; i < callerCount; i++)
        {
            if (i != remaining)
            {
                cancels[i].Cancel();
            }
        }
        await Task.Delay(500); // cancellation happens on a worker; need to allow a moment
        for (int i = 0; i < callerCount; i++)
        {
            if (i != remaining)
            {
                var result = results[i];
                // should have already cancelled, even though underlying task hasn't finished yet
                Assert.Equal(TaskStatus.Canceled, result.Status);
                var ex = Assert.Throws<OperationCanceledException>(() => result.GetAwaiter().GetResult());
                Assert.Equal(cancels[i].Token, ex.CancellationToken); // each gets the correct blame
            }
        }

        Assert.Equal(0, Volatile.Read(ref executeCount));
        semaphore.Release();
        if (!await semaphore.WaitAsync(5_000)) // wait for underlying task to hand back to us
        {
            throw new TimeoutException("Didn't get handshake back from task");
        }
        Assert.Equal(1, Volatile.Read(ref executeCount));
        Assert.Equal(0, Volatile.Read(ref cancelCount)); // ran to completion
        await results[remaining];
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
