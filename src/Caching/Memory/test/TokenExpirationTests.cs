// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class TokenExpirationTests
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
        public void SetWithTokenRegistersForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, expirationToken);

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.True(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.NotNull(expirationToken.Registration);
            Assert.NotNull(expirationToken.Registration.RegisteredCallback);
            Assert.NotNull(expirationToken.Registration.RegisteredState);
            Assert.False(expirationToken.Registration.Disposed);
        }

        [Fact]
        public void SetWithLazyTokenDoesntRegisterForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.True(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.Null(expirationToken.Registration);
        }

        [Fact]
        public void FireTokenRemovesItem()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            expirationToken.Fire();

            var found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemOnNextAccess()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            expirationToken.HasChanged = true;

            found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void ExpiredLazyTokenRemovesItemInBackground()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            clock.Add(TimeSpan.FromMinutes(2));
            expirationToken.HasChanged = true;
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            found = cache.TryGetValue(key, out value);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesTokenRegistration()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { ActiveChangeCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            cache.Remove(key);

            Assert.NotNull(expirationToken.Registration);
            Assert.True(expirationToken.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");
        }

        [Fact]
        public void AddExpiredTokenPreventsCaching()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestExpirationToken() { HasChanged = true };
            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            Assert.Same(value, result); // The created item should be returned, but not cached.

            Assert.True(expirationToken.HasChangedWasCalled);
            Assert.False(expirationToken.ActiveChangeCallbacksWasCalled);
            Assert.Null(expirationToken.Registration);
            Assert.True(callbackInvoked.WaitOne(TimeSpan.FromSeconds(30)), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }

        [Fact]
        public void TokenExpiresOnRegister()
        {
            var cache = CreateCache();
            var key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var expirationToken = new TestToken(callbackInvoked);
            var task = Task.Run(() => cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationToken(expirationToken)));
            callbackInvoked.WaitOne(TimeSpan.FromSeconds(30));
            var result = task.Result;

            Assert.Same(value, result);
            result = cache.Get(key);
            Assert.Null(result);
        }

        internal class TestToken : IChangeToken
        {
            private bool _hasChanged;
            private ManualResetEvent _event;

            public TestToken(ManualResetEvent mre)
            {
                _event = mre;
            }

            public bool ActiveChangeCallbacks
            {
                get
                {
                    return true;
                }
            }

            public bool HasChanged
            {
                get
                {
                    return _hasChanged;
                }
            }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _hasChanged = true;
                callback(state);
                _event.Set();
                return new TestDisposable();
            }
        }

        internal class TestDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
