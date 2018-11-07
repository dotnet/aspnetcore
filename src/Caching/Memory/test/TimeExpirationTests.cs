// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class TimeExpirationTests
    {
        private IMemoryCache CreateCache(ISystemClock clock)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
            });
        }

        [Fact]
        public void AbsoluteExpirationInThePastDoesntAddEntry()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var obj = new object();

            var expected = clock.UtcNow - TimeSpan.FromMinutes(1);
            var result = cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expected));

            Assert.Null(cache.Get(key));
        }

        [Fact]
        public void AbsoluteExpirationExpires()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            var result = cache.Set(key, value, clock.UtcNow + TimeSpan.FromMinutes(1));
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            clock.Add(TimeSpan.FromMinutes(2));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void AbsoluteExpirationExpiresInBackground()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(clock.UtcNow + TimeSpan.FromMinutes(1))
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, callbackInvoked);
            var result = cache.Set(key, value, options);
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            clock.Add(TimeSpan.FromMinutes(2));
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeRelativeExpirationThrows()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
            },
            nameof(MemoryCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroRelativeExpirationThrows()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
            },
            nameof(MemoryCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.Zero);
        }

        [Fact]
        public void RelativeExpirationExpires()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            var result = cache.Set(key, value, TimeSpan.FromMinutes(1));
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            clock.Add(TimeSpan.FromMinutes(2));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeSlidingExpirationThrows()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
            },
            nameof(MemoryCacheEntryOptions.SlidingExpiration),
            "The sliding expiration value must be positive.",
            TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroSlidingExpirationThrows()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.Zero));
            },
            nameof(MemoryCacheEntryOptions.SlidingExpiration),
            "The sliding expiration value must be positive.",
            TimeSpan.Zero);
        }

        [Fact]
        public void SlidingExpirationExpiresIfNotAccessed()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            var result = cache.Set(key, value, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            clock.Add(TimeSpan.FromMinutes(2));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccess()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(1)));
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            for (int i = 0; i < 10; i++)
            {
                clock.Add(TimeSpan.FromSeconds(15));

                found = cache.TryGetValue(key, out result);
                Assert.True(found);
                Assert.Same(value, result);
            }
        }

        [Fact]
        public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var key = "myKey";
            var value = new object();

            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));
            Assert.Same(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Same(value, result);

            for (int i = 0; i < 7; i++)
            {
                clock.Add(TimeSpan.FromSeconds(15));

                found = cache.TryGetValue(key, out result);
                Assert.True(found);
                Assert.Same(value, result);
            }

            clock.Add(TimeSpan.FromSeconds(15));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }
    }
}