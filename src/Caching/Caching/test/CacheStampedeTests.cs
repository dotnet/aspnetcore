// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Distributed.Tests;
public class CacheStampedeTests
{
    private HybridCache GetCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task NonCancellableCallersGetSameTask()
    {
        TaskCompletionSource<string> pendingResult = new();
        var cache = GetCache();
        var a = cache.GetOrCreateAsync<string>("dummy", _ => new(pendingResult.Task)).AsTask();
        var b = cache.GetOrCreateAsync<string>("dummy", _ => throw new InvalidOperationException("should not be used")).AsTask();

        Assert.False(a.IsCompleted);
        Assert.False(b.IsCompleted);
        Assert.Same(a, b); // should have reused internal implementation
        Assert.NotSame(a, pendingResult.Task);

        pendingResult.SetResult("abc");
        Assert.Equal("abc", await a);
        Assert.Equal("abc", await b); // note same task
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]

    public async Task LocalCancellationFirstCaller(bool cancel)
    {
        TaskCompletionSource<string> pendingResult = new();
        CancellationTokenSource cts = new();
        if (cancel) cts.CancelAfter(100);
        var cache = GetCache();
        var a = cache.GetOrCreateAsync<string>("dummy", _ => new(pendingResult.Task), cancellationToken: cts.Token).AsTask();
        var b = cache.GetOrCreateAsync<string>("dummy", _ => throw new InvalidOperationException("should not be used")).AsTask();

        Assert.False(a.IsCompleted);
        Assert.False(b.IsCompleted);
        Assert.NotSame(a, b); // can't share because a needs to support cancellation

        if (cancel)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await a);
        }

        pendingResult.SetResult("abc");
        Assert.Equal("abc", await b);

        if (!cancel)
        {
            Assert.Equal("abc", await a);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]

    public async Task LocalCancellationSecondCaller(bool cancel)
    {
        TaskCompletionSource<string> pendingResult = new();
        CancellationTokenSource cts = new();
        if (cancel) cts.CancelAfter(100);
        var cache = GetCache();
        var a = cache.GetOrCreateAsync<string>("dummy", _ => new(pendingResult.Task)).AsTask();
        var b = cache.GetOrCreateAsync<string>("dummy", _ => throw new InvalidOperationException("should not be used"), cancellationToken: cts.Token).AsTask();

        Assert.False(a.IsCompleted);
        Assert.False(b.IsCompleted);
        Assert.NotSame(a, b); // can't share because b needs to support cancellation

        if (cancel)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await b);
        }

        pendingResult.SetResult("abc");
        Assert.Equal("abc", await a);

        if (!cancel)
        {
            Assert.Equal("abc", await b);
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]

    public async Task LocalCancellationBothCallers(bool cancelFirst, bool cancelSecond)
    {
        TaskCompletionSource<string> pendingResult = new();
        CancellationTokenSource firstCts = new(), secondCts = new();
        if (cancelFirst) firstCts.CancelAfter(100);
        if (cancelSecond) secondCts.CancelAfter(100);
        var cache = GetCache();

        bool hasBeenInvoked = false, cancellationObserved = false;
        var a = cache.GetOrCreateAsync<string>("dummy", async ct => {
            Volatile.Write(ref hasBeenInvoked, true);
            ct.Register(() =>
            {
                Volatile.Write(ref cancellationObserved, true);
            });
            return await pendingResult.Task;
        }).AsTask();
        var b = cache.GetOrCreateAsync<string>("dummy", _ => throw new InvalidOperationException("should not be used"), cancellationToken: secondCts.Token).AsTask();

        Assert.False(a.IsCompleted);
        Assert.False(b.IsCompleted);
        Assert.NotSame(a, b); // can't share because both need to support cancellation

        if (cancelFirst)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await a);
        }
        if (cancelSecond)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await b);
        }
        pendingResult.SetResult("abc");

        if (!cancelFirst)
        {
            Assert.Equal("abc", await a);
        }
        if (!cancelSecond)
        {
            Assert.Equal("abc", await b);
        }
        Assert.True(Volatile.Read(ref hasBeenInvoked), "was invoked");
        Assert.Equal(cancelFirst && cancelSecond, Volatile.Read(ref cancellationObserved));
    }
}
