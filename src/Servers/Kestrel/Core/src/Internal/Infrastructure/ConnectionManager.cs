// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class ConnectionManager : IHeartbeatHandler
{
    private readonly Action<KestrelConnection> _walkCallback;

    private long _lastConnectionId = long.MinValue;

    private readonly ConcurrentDictionary<long, ConnectionReference> _connectionReferences = new ConcurrentDictionary<long, ConnectionReference>();
    private readonly KestrelTrace _trace;

    public ConnectionManager(KestrelTrace trace, long? upgradedConnectionLimit)
        : this(trace, GetCounter(upgradedConnectionLimit))
    {
    }

    public ConnectionManager(KestrelTrace trace, ResourceCounter upgradedConnections)
    {
        UpgradedConnectionCount = upgradedConnections;
        _trace = trace;
        _walkCallback = WalkCallback;
    }

    public long GetNewConnectionId() => Interlocked.Increment(ref _lastConnectionId);

    /// <summary>
    /// Connections that have been switched to a different protocol.
    /// </summary>
    public ResourceCounter UpgradedConnectionCount { get; }

    public void OnHeartbeat()
    {
        Walk(_walkCallback);
    }

    private void WalkCallback(KestrelConnection connection)
    {
        connection.TickHeartbeat();
    }

    public void AddConnection(long id, ConnectionReference connectionReference)
    {
        if (!_connectionReferences.TryAdd(id, connectionReference))
        {
            throw new ArgumentException("Unable to add connection.", nameof(id));
        }
    }

    public void RemoveConnection(long id)
    {
        if (!_connectionReferences.TryRemove(id, out var reference))
        {
            throw new ArgumentException("Unable to remove connection.", nameof(id));
        }

        if (reference.TryGetConnection(out var connection))
        {
            connection.Complete();
        }
    }

    public void Walk(Action<KestrelConnection> callback)
    {
        foreach (var kvp in _connectionReferences)
        {
            var reference = kvp.Value;

            if (reference.TryGetConnection(out var connection))
            {
                callback(connection);
            }
            else if (_connectionReferences.TryRemove(kvp.Key, out reference))
            {
                // It's safe to modify the ConcurrentDictionary in the foreach.
                // The connection reference has become unrooted because the application never completed.
                _trace.ApplicationNeverCompleted(reference.ConnectionId);
                reference.StopTransportTracking();
            }

            // If both conditions are false, the connection was removed during the heartbeat.
        }
    }

    private static ResourceCounter GetCounter(long? number)
        => number.HasValue
            ? ResourceCounter.Quota(number.Value)
            : ResourceCounter.Unlimited;
}
