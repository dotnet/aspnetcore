// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Xunit;

namespace System.Text;

public class StringBuilderCacheTest
{
    [Fact]
    public void AcquireReturnsEmptyBuilder()
    {
        var sb = StringBuilderCache.Acquire();

        Assert.Equal(0, sb.Length);
        StringBuilderCache.Release(sb);
    }

    [Fact]
    public void GetStringAndReleaseReturnsContentsAndCachesBuilder()
    {
        var sb = StringBuilderCache.Acquire();
        sb.Append("max-age=10");

        Assert.Equal("max-age=10", StringBuilderCache.GetStringAndRelease(sb));

        var next = StringBuilderCache.Acquire();
        Assert.Same(sb, next);
        Assert.Equal(0, next.Length);
        StringBuilderCache.Release(next);
    }

    [Fact]
    public void AcquireAfterAcquireReturnsDifferentBuilder()
    {
        var first = StringBuilderCache.Acquire();
        var second = StringBuilderCache.Acquire();

        Assert.NotSame(first, second);
        StringBuilderCache.Release(first);
    }

    [Fact]
    public void ReleaseDoesNotCacheBuilderLargerThanMaxBuilderSize()
    {
        var sb = StringBuilderCache.Acquire();
        sb.Append('a', StringBuilderCache.MaxBuilderSize + 1);

        Assert.Equal(StringBuilderCache.MaxBuilderSize + 1, StringBuilderCache.GetStringAndRelease(sb).Length);
        Assert.NotSame(sb, StringBuilderCache.Acquire());
    }

    [Fact]
    public void AcquireWithCapacityLargerThanCachedBuilderReturnsNewBuilder()
    {
        var sb = new StringBuilder(16);
        StringBuilderCache.Release(sb);

        var larger = StringBuilderCache.Acquire(100);

        Assert.NotSame(sb, larger);
        Assert.True(larger.Capacity >= 100);
        Assert.Same(sb, StringBuilderCache.Acquire());
    }

    [Fact]
    public void AcquireWithCapacityLargerThanMaxBuilderSizeReturnsNewBuilder()
    {
        var sb = StringBuilderCache.Acquire();
        StringBuilderCache.Release(sb);

        var large = StringBuilderCache.Acquire(StringBuilderCache.MaxBuilderSize + 1);

        Assert.NotSame(sb, large);
        Assert.Same(sb, StringBuilderCache.Acquire());
    }

    [Fact]
    public void CachedBuilderIsPerThread()
    {
        var sb = StringBuilderCache.Acquire();
        StringBuilderCache.Release(sb);

        StringBuilder? other = null;
        var thread = new Thread(() => other = StringBuilderCache.Acquire());
        thread.Start();
        thread.Join();

        Assert.NotNull(other);
        Assert.NotSame(sb, other);
        Assert.Same(sb, StringBuilderCache.Acquire());
    }
}
