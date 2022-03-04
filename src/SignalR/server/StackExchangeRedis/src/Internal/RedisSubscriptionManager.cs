// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    internal class RedisSubscriptionManager
    {
        private readonly ConcurrentDictionary<string, HubConnectionStore> _subscriptions = new ConcurrentDictionary<string, HubConnectionStore>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public async Task AddSubscriptionAsync(string id, HubConnectionContext connection, Func<string, HubConnectionStore, Task> subscribeMethod)
        {
            await _lock.WaitAsync();

            try
            {
                var subscription = _subscriptions.GetOrAdd(id, _ => new HubConnectionStore());

                subscription.Add(connection);

                // Subscribe once
                if (subscription.Count == 1)
                {
                    await subscribeMethod(id, subscription);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RemoveSubscriptionAsync(string id, HubConnectionContext connection, Func<string, Task> unsubscribeMethod)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_subscriptions.TryGetValue(id, out var subscription))
                {
                    return;
                }

                subscription.Remove(connection);

                if (subscription.Count == 0)
                {
                    _subscriptions.TryRemove(id, out _);
                    await unsubscribeMethod(id);
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
