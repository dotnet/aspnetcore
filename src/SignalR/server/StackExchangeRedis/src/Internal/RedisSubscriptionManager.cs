// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal sealed class RedisSubscriptionManager
{
    private readonly ConcurrentDictionary<string, HubConnectionStore> _subscriptions = new ConcurrentDictionary<string, HubConnectionStore>(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public async Task AddGroupSubscriptionAsync(string channelName, string groupName, HubConnectionContext connection, Func<string, HubConnectionStore, Task> subscribeMethod)
    {
        await _lock.WaitAsync();
        try
        {
            var feature = connection.Features.GetRequiredFeature<IRedisFeature>();
            if (feature.IsDisconnected)
            {
                return;
            }

            var groupNames = feature.Groups;

            // Connection already in group
            if (!groupNames.Add(groupName))
            {
                return;
            }

            try
            {
                if (!await AddSubscriptionAsyncCore(channelName, connection, subscribeMethod))
                {
                    groupNames.Remove(groupName);
                }
            }
            catch
            {
                groupNames.Remove(groupName);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddSubscriptionAsync(string channelName, HubConnectionContext connection, Func<string, HubConnectionStore, Task> subscribeMethod)
    {
        await _lock.WaitAsync();
        try
        {
            await AddSubscriptionAsyncCore(channelName, connection, subscribeMethod);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveGroupSubscriptionAsync(string channelName, string groupName, HubConnectionContext connection, object state, Func<object, string, Task> unsubscribeMethod)
    {
        await _lock.WaitAsync();
        try
        {
            var groupNames = connection.Features.GetRequiredFeature<IRedisFeature>().Groups;

            if (!groupNames.Remove(groupName))
            {
                // Group not tracked by this connection, so no need to remove it from the subscription list
                return;
            }

            await RemoveSubscriptionAsyncCore(channelName, connection, state, unsubscribeMethod);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string[]> SetConnectionDisconnectedAndGetGroupNamesAsync(HubConnectionContext connection)
    {
        await _lock.WaitAsync();
        try
        {
            var feature = connection.Features.GetRequiredFeature<IRedisFeature>();
            feature.IsDisconnected = true;
            return feature.Groups.ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveSubscriptionAsync(string channelName, HubConnectionContext connection, object state, Func<object, string, Task> unsubscribeMethod)
    {
        await _lock.WaitAsync();
        try
        {
            await RemoveSubscriptionAsyncCore(channelName, connection, state, unsubscribeMethod);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<bool> AddSubscriptionAsyncCore(string id, HubConnectionContext connection, Func<string, HubConnectionStore, Task> subscribeMethod)
    {
        Debug.Assert(_lock.CurrentCount == 0);

        // Avoid adding subscription if connection is closing/closed
        // We're in a lock and ConnectionAborted is triggered before OnDisconnectedAsync is called so this is guaranteed to be safe when adding while connection is closing and removing items
        if (connection.ConnectionAborted.IsCancellationRequested)
        {
            return false;
        }

        var subscription = _subscriptions.GetOrAdd(id, _ => new HubConnectionStore());

        subscription.Add(connection);

        // Subscribe once
        if (subscription.Count == 1)
        {
            try
            {
                await subscribeMethod(id, subscription);
            }
            catch
            {
                subscription.Remove(connection);
                if (subscription.Count == 0)
                {
                    _subscriptions.TryRemove(id, out _);
                }

                throw;
            }
        }

        return true;
    }

    private async Task RemoveSubscriptionAsyncCore(string id, HubConnectionContext connection, object state, Func<object, string, Task> unsubscribeMethod)
    {
        Debug.Assert(_lock.CurrentCount == 0);

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
}
