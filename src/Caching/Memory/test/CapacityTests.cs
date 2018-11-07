// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CapacityTests
    {
        [Fact]
        public void MemoryDistributedCacheOptionsDefaultsTo200MBSizeLimit()
        {
            Assert.Equal(200 * 1024 * 1024, new MemoryDistributedCacheOptions().SizeLimit);
        }

        [Fact]
        public void NegativeSizeOnMemoryCacheEntryOptionsThrows()
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntryOptions.Size = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntryOptions.SetSize(-1); });
        }

        [Fact]
        public void NegativeSizeOnMemoryCacheEntryThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            using (var cacheEntry = cache.CreateEntry(new object()))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.Size = -1; });
                Assert.Throws<ArgumentOutOfRangeException>(() => { cacheEntry.SetSize(-1); });
            }
        }

        [Fact]
        public void CacheWithSizeLimitAddingEntryWithoutSizeThrows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10
            });

            Assert.Throws<InvalidOperationException>(() => cache.Set(new object(), new object()));
        }

        [Fact]
        public void NonPositiveCacheSizeLimitThrows()
        {
            var options = new MemoryCacheOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.SizeLimit = -1);
        }

        [Fact]
        public void InvalidRemovalPercentageOnOvercapacityCompactionThrows()
        {
            var options = new MemoryCacheOptions();

            Assert.Throws<ArgumentOutOfRangeException>(() => options.CompactionPercentage = 1.1);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.CompactionPercentage = -0.1);
        }

        [Fact]
        public void AddingEntryIncreasesCacheSizeWhenEnforcingSizeLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            Assert.Equal(5, cache.Size);
        }

        [Fact]
        public void AddingEntryDoesNotIncreasesCacheSizeWhenNotEnforcingSizeLimit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public void DoNotAddEntryIfItExceedsCapacity()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 4 });

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(4, cache.Size);

            cache.Set("key2", "value2", new MemoryCacheEntryOptions { Size = 7 });

            Assert.Null(cache.Get("key2"));
            Assert.Equal(4, cache.Size);
        }

        [Fact]
        public async Task DoNotAddIfSizeOverflows()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = long.MaxValue
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = long.MaxValue };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(long.MaxValue, cache.Size);

            cache.Set("key1", "value1", new MemoryCacheEntryOptions { Size = long.MaxValue });
            // Do not add the new item
            Assert.Null(cache.Get("key1"));

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            // Compaction removes old item
            Assert.Null(cache.Get("key"));
            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public async Task ExceedsCapacityCompacts()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 6 };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(6, cache.Size);

            cache.Set("key2", "value2", new MemoryCacheEntryOptions { Size = 5 });

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Null(cache.Get("key"));
            Assert.Null(cache.Get("key2"));
            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public void AddingReplacementWithSizeIncreaseUpdates()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(2, cache.Size);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 3 });

            Assert.Equal("value1", cache.Get("key"));
            Assert.Equal(3, cache.Size);
        }

        [Fact]
        public void AddingReplacementWithSizeDecreaseUpdates()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 2 });

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(2, cache.Size);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 1 });

            Assert.Equal("value1", cache.Get("key"));
            Assert.Equal(1, cache.Size);
        }

        [Fact]
        public void AddingReplacementWhenTotalSizeExceedsCapacityDoesNotUpdateAndRemovesOldEntry()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(5, cache.Size);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 6 });

            Assert.Null(cache.Get("key"));
            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public async Task AddingReplacementWhenTotalSizeExceedsCapacityDoesNotUpdateRemovesOldEntryAndTriggersCompaction()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 6 };
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", entryOptions);

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(6, cache.Size);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 5 });

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Null(cache.Get("key"));
            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public void AddingReplacementExceedsCapacityRemovesOldEntry()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 10,
                CompactionPercentage = 0.5
            });

            Assert.Equal(0, cache.Size);

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 6 });

            Assert.Equal("value", cache.Get("key"));
            Assert.Equal(6, cache.Size);

            cache.Set("key", "value1", new MemoryCacheEntryOptions { Size = 11 });

            Assert.Null(cache.Get("key"));
            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public void RemovingEntryDecreasesCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });

            cache.Set("key", "value", new MemoryCacheEntryOptions { Size = 5 });

            Assert.Equal(5, cache.Size);

            cache.Remove("key");

            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public async Task ExpiringEntryDecreasesCacheSize()
        {
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.Zero,
                SizeLimit = 10
            });

            var entryOptions = new MemoryCacheEntryOptions { Size = 5 };
            var changeToken = new TestExpirationToken();
            var sem = new SemaphoreSlim(0, 1);
            entryOptions.ExpirationTokens.Add(changeToken);
            entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (k, v, r, s) => sem.Release(),
                State = null
            });

            cache.Set("key", "value", entryOptions);

            Assert.Equal(5, cache.Size);

            // Expire entry
            changeToken.Fire();

            // Trigger compaction
            Assert.Null(cache.Get("key"));

            // Wait for compaction to complete
            Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));

            Assert.Equal(0, cache.Size);
        }

        [Fact]
        public async Task CompactsToLessThanLowWatermarkUsingLRUWhenHighWatermarkExceeded()
        {
            var testClock = new TestClock();
            var cache = new MemoryCache(new MemoryCacheOptions
            {
                Clock = testClock,
                SizeLimit = 10,
                CompactionPercentage = 0.3
            });

            var numEntries = 5;
            var sem = new SemaphoreSlim(0, numEntries);

            for (var i = 0; i < numEntries; i++)
            {
                var entryOptions = new MemoryCacheEntryOptions { Size = i };
                entryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (k, v, r, s) => sem.Release(),
                    State = null
                });
                cache.Set($"key{i}", $"value{i}", entryOptions);
                testClock.Add(TimeSpan.FromSeconds(1));
            }

            // There should be 5 items in the cache
            Assert.Equal(numEntries, cache.Count);

            cache.Set($"key{numEntries}", $"value{numEntries}", new MemoryCacheEntryOptions { Size = 1 });
            testClock.Add(TimeSpan.FromSeconds(10));

            // Wait for compaction to complete
            for (var i = 0; i < 3; i++)
            {
                Assert.True(await sem.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            // There should be 2 items in the cache
            Assert.Equal(2, cache.Count);
            Assert.Null(cache.Get("key0"));
            Assert.Null(cache.Get("key1"));
            Assert.Null(cache.Get("key2"));
            Assert.NotNull(cache.Get("key3"));
            Assert.NotNull(cache.Get("key4"));
            Assert.Null(cache.Get("key5"));
        }

        [Fact]
        public void NoCompactionWhenNoMaximumEntriesCountSpecified()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());

            for (var i = 0; i < 5; i++)
            {
                cache.Set($"key{i}", $"value{i}", new MemoryCacheEntryOptions { Size = 1 });
            }

            // There should be 5 items in the cache
            Assert.Equal(5, cache.Count);

            cache.Set("key5", "value5", new MemoryCacheEntryOptions { Size = 1 });

            // There should be 6 items in the cache
            Assert.Equal(6, cache.Count);
        }
    }
}