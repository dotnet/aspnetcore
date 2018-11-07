// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class CacheEntryScopeExpirationTests
    {
        private IMemoryCache CreateCache()
        {
            return CreateCache(new SystemClock());
        }

        private IMemoryCache CreateCache(ISystemClock clock)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
            });
        }

        [Fact]
        public void SetPopulates_ExpirationTokens_IntoScopedLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                Assert.Same(entry, CacheEntryHelper.Current);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Single(((CacheEntry)entry)._expirationTokens);
            Assert.Null(((CacheEntry)entry)._absoluteExpiration);
        }

        [Fact]
        public void SetPopulates_AbsoluteExpiration_IntoScopeLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            var time = new DateTimeOffset(2051, 1, 1, 1, 1, 1, TimeSpan.Zero);

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                Assert.Same(entry, CacheEntryHelper.Current);

                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(time));
            }

            Assert.Null(((CacheEntry)entry)._expirationTokens);
            Assert.NotNull(((CacheEntry)entry)._absoluteExpiration);
            Assert.Equal(time, ((CacheEntry)entry)._absoluteExpiration);
        }

        [Fact]
        public void TokenExpires_LinkedEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenExpires_GetInLinkedEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            cache.GetOrCreate(key1, e =>
            {
                e.AddExpirationToken(expirationToken);
                return obj;
            });

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(cache.Get(key1));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenExpires_ParentScopeEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                using (var entry1 = cache.CreateEntry(key1))
                {
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenExpires_ParentScopeEntry_WithFactory()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            cache.GetOrCreate(key, entry =>
            {
                cache.GetOrCreate(key1, entry1 =>
                {
                    entry1.AddExpirationToken(expirationToken);
                    return obj;
                });

                return obj;
            });

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void TokenDoesntExpire_SiblingScopeEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);

                using (var entry1 = cache.CreateEntry(key1))
                {
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                }
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            expirationToken.Fire();

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
            Assert.True(cache.TryGetValue(key2, out value));
        }

        [Fact]
        public void AbsoluteExpiration_WorksAcrossLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry = cache.CreateEntry(key))
            {
                entry.SetValue(obj);
                cache.Set(key1, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));
            }

            Assert.Same(obj, cache.Get(key));
            Assert.Same(obj, cache.Get(key1));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void AbsoluteExpiration_WorksAcrossNestedLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2.SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key2, out value));
        }


        [Fact]
        public void AbsoluteExpiration_DoesntAffectSiblingLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            string key3 = "myKey3";
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };

            using (var entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);

                using (var entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2.SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                }

                using (var entry3 = cache.CreateEntry(key3))
                {
                    entry3.SetValue(obj);
                    entry3.SetAbsoluteExpiration(TimeSpan.FromSeconds(15));
                }
            }

            Assert.Same(obj, cache.Get(key1));
            Assert.Same(obj, cache.Get(key2));
            Assert.Same(obj, cache.Get(key3));

            clock.Add(TimeSpan.FromSeconds(10));

            Assert.False(cache.TryGetValue(key1, out object value));
            Assert.False(cache.TryGetValue(key2, out value));
            Assert.True(cache.TryGetValue(key3, out value));
        }

        [Fact]
        public void GetWithImplicitLinkPopulatesExpirationTokens()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(CacheEntryHelper.Current);

            ICacheEntry entry;
            using (entry = cache.CreateEntry(key))
            {
                Assert.Same(entry, CacheEntryHelper.Current);
                var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            Assert.Null(CacheEntryHelper.Current);

            Assert.Single(((CacheEntry)entry)._expirationTokens);
            Assert.Null(((CacheEntry)entry)._absoluteExpiration);
        }

        [Fact]
        public void LinkContextsCanNest()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(CacheEntryHelper.Current);

            ICacheEntry entry;
            ICacheEntry entry1;
            using (entry = cache.CreateEntry(key))
            {
                Assert.Same(entry, CacheEntryHelper.Current);

                using (entry1 = cache.CreateEntry(key1))
                {
                    Assert.Same(entry1, CacheEntryHelper.Current);

                    var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
                    entry1.SetValue(obj);
                    entry1.AddExpirationToken(expirationToken);
                }

                Assert.Same(entry, CacheEntryHelper.Current);
            }

            Assert.Null(CacheEntryHelper.Current);

            Assert.Single(((CacheEntry)entry1)._expirationTokens);
            Assert.Null(((CacheEntry)entry1)._absoluteExpiration);
            Assert.Single(((CacheEntry)entry)._expirationTokens);
            Assert.Null(((CacheEntry)entry)._absoluteExpiration);
        }

        [Fact]
        public void NestedLinkContextsCanAggregate()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";

            var expirationToken1 = new TestExpirationToken() { ActiveChangeCallbacks = true };
            var expirationToken2 = new TestExpirationToken() { ActiveChangeCallbacks = true };

            ICacheEntry entry1 = null;
            ICacheEntry entry2 = null;

            using (entry1 = cache.CreateEntry(key1))
            {
                entry1.SetValue(obj);
                entry1
                    .AddExpirationToken(expirationToken1)
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

                using (entry2 = cache.CreateEntry(key2))
                {
                    entry2.SetValue(obj);
                    entry2
                        .AddExpirationToken(expirationToken2)
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(15));
                }
            }

            Assert.Equal(2, ((CacheEntry)entry1)._expirationTokens.Count());
            Assert.NotNull(((CacheEntry)entry1)._absoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(10), ((CacheEntry)entry1)._absoluteExpiration);

            Assert.Single(((CacheEntry)entry2)._expirationTokens);
            Assert.NotNull(((CacheEntry)entry2)._absoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(15), ((CacheEntry)entry2)._absoluteExpiration);
        }

        [Fact]
        public async Task LinkContexts_AreThreadSafe()
        {
            var cache = CreateCache();
            var key1 = new object();
            var key2 = new object();
            var key3 = new object();
            var key4 = new object();
            var value1 = Guid.NewGuid();
            var value2 = Guid.NewGuid();
            var value3 = Guid.NewGuid();
            var value4 = Guid.NewGuid();
            TestExpirationToken t3 = null;
            TestExpirationToken t4 = null;

            Func<Task> func = async () =>
            {
                t3 = new TestExpirationToken() { ActiveChangeCallbacks = true };
                t4 = new TestExpirationToken() { ActiveChangeCallbacks = true };

                value1 = await cache.GetOrCreateAsync(key1, async e1 =>
                {
                    value2 = await cache.GetOrCreateAsync(key2, async e2 =>
                    {
                        await Task.WhenAll(
                            Task.Run(() =>
                            {
                                value3 = cache.Set(key3, Guid.NewGuid(), t3);
                            }),
                            Task.Run(() =>
                            {
                                value4 = cache.Set(key4, Guid.NewGuid(), t4);
                            }));

                        return Guid.NewGuid();
                    });

                    return Guid.NewGuid();
                });
            };

            await func();

            Assert.NotNull(cache.Get(key1));
            Assert.NotNull(cache.Get(key2));
            Assert.Equal(value3, cache.Get(key3));
            Assert.Equal(value4, cache.Get(key4));
            Assert.NotEqual(value3, value4);

            t3.Fire();
            Assert.Equal(value4, cache.Get(key4));

            Assert.Null(cache.Get(key1));
            Assert.Null(cache.Get(key2));
            Assert.Null(cache.Get(key3));

            await func();

            Assert.NotNull(cache.Get(key1));
            Assert.NotNull(cache.Get(key2));
            Assert.Equal(value3, cache.Get(key3));
            Assert.Equal(value4, cache.Get(key4));
            Assert.NotEqual(value3, value4);

            t4.Fire();
            Assert.Equal(value3, cache.Get(key3));

            Assert.Null(cache.Get(key1));
            Assert.Null(cache.Get(key2));
            Assert.Null(cache.Get(key4));
        }
    }
}
