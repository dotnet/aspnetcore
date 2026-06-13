// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features;

public class FeatureReferencesFetchTests
{
    private interface ITestFeature
    {
        string Name { get; }
    }

    private class TestFeature : ITestFeature
    {
        public string Name { get; set; } = "default";
    }

    private struct TestCache
    {
        public ITestFeature? Feature;
    }

    /// <summary>
    /// Regression test for https://github.com/dotnet/aspnetcore/issues/42040.
    /// Concurrent calls to Fetch while the feature collection revision changes
    /// must not return null.
    /// </summary>
    [Fact]
    public void Fetch_ConcurrentRevisionChange_DoesNotReturnNull()
    {
        const int threadCount = 8;
        const int iterations = 200_000;

        var collection = new FeatureCollection();
        collection.Set<ITestFeature>(new TestFeature { Name = "initial" });

        var refs = new FeatureReferences<TestCache>(collection);
        Func<IFeatureCollection, TestFeature> factory = _ => new TestFeature { Name = "created" };

        var barrier = new Barrier(threadCount);
        var exceptions = new Exception?[threadCount];

        var threads = new Thread[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            threads[t] = new Thread(() =>
            {
                barrier.SignalAndWait();
                try
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        // Half the threads bump the revision by setting a feature,
                        // the other half read via Fetch.
                        if (threadIndex % 2 == 0)
                        {
                            collection.Set<ITestFeature>(new TestFeature { Name = $"v{i}" });
                        }
                        else
                        {
                            var result = refs.Fetch(ref refs.Cache.Feature, factory);
                            Assert.NotNull(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions[threadIndex] = ex;
                }
            });
            threads[t].Start();
        }

        for (var t = 0; t < threadCount; t++)
        {
            threads[t].Join();
        }

        foreach (var ex in exceptions)
        {
            if (ex is not null)
            {
                throw new AggregateException("Fetch returned null or threw during concurrent access", ex);
            }
        }
    }

    /// <summary>
    /// Fetch returns the cached value on the fast path (revision unchanged).
    /// </summary>
    [Fact]
    public void Fetch_RevisionUnchanged_ReturnsCachedValue()
    {
        var collection = new FeatureCollection();
        collection.Set<ITestFeature>(new TestFeature { Name = "original" });

        var refs = new FeatureReferences<TestCache>(collection);
        Func<IFeatureCollection, TestFeature> factory = _ => new TestFeature { Name = "should not be used" };

        var first = refs.Fetch(ref refs.Cache.Feature, factory);
        var second = refs.Fetch(ref refs.Cache.Feature, factory);

        Assert.Same(first, second);
    }

    /// <summary>
    /// Fetch refreshes the cache when the collection revision changes.
    /// </summary>
    [Fact]
    public void Fetch_RevisionChanged_RefreshesCache()
    {
        var collection = new FeatureCollection();
        collection.Set<ITestFeature>(new TestFeature { Name = "v1" });

        var refs = new FeatureReferences<TestCache>(collection);
        Func<IFeatureCollection, TestFeature> factory = _ => new TestFeature { Name = "factory" };

        var first = refs.Fetch(ref refs.Cache.Feature, factory);
        Assert.Equal("v1", first!.Name);

        // Bump revision by setting a new feature value.
        collection.Set<ITestFeature>(new TestFeature { Name = "v2" });

        var second = refs.Fetch(ref refs.Cache.Feature, factory);
        Assert.NotNull(second);
        Assert.Equal("v2", second!.Name);
        Assert.NotSame(first, second);
    }

    /// <summary>
    /// Fetch uses the factory when the feature is not in the collection.
    /// </summary>
    [Fact]
    public void Fetch_FeatureNotInCollection_UsesFactory()
    {
        var collection = new FeatureCollection();
        var refs = new FeatureReferences<TestCache>(collection);
        Func<IFeatureCollection, TestFeature> factory = _ => new TestFeature { Name = "from factory" };

        var result = refs.Fetch(ref refs.Cache.Feature, factory);

        Assert.NotNull(result);
        Assert.Equal("from factory", result!.Name);
    }
}
