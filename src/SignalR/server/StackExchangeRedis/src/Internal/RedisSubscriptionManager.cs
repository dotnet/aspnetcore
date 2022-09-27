// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal sealed class RedisSubscriptionManager
{
    private readonly ConcurrentDictionary<string, HubConnectionStore> _subscriptions = new ConcurrentDictionary<string, HubConnectionStore>(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public async Task AddSubscriptionAsync(string id, HubConnectionContext connection, Func<string, HubConnectionStore, Task> subscribeMethod)
    {
        await _lock.WaitAsync();

        try
        {
            // Avoid adding subscription if connection is closing/closed
            // We're in a lock and ConnectionAborted is triggered before OnDisconnectedAsync is called so this is guaranteed to be safe when adding while connection is closing and removing items
            if (connection.ConnectionAborted.IsCancellationRequested)
            {
                return;
            }

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

    public async Task RemoveSubscriptionAsync(string id, HubConnectionContext connection, object state, Func<object, string, Task> unsubscribeMethod)
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
                await unsubscribeMethod(state, id);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
