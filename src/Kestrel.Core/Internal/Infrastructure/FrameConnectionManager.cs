// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class FrameConnectionManager
    {
        private readonly ConcurrentDictionary<long, FrameConnectionReference> _connectionReferences = new ConcurrentDictionary<long, FrameConnectionReference>();
        private readonly IKestrelTrace _trace;

        public FrameConnectionManager(IKestrelTrace trace, long? upgradedConnectionLimit)
            : this(trace, GetCounter(upgradedConnectionLimit))
        {
        }

        public FrameConnectionManager(IKestrelTrace trace, ResourceCounter upgradedConnections)
        {
            UpgradedConnectionCount = upgradedConnections;
            _trace = trace;
        }

        /// <summary>
        /// Connections that have been switched to a different protocol.
        /// </summary>
        public ResourceCounter UpgradedConnectionCount { get; }

        public void AddConnection(long id, FrameConnection connection)
        {
            if (!_connectionReferences.TryAdd(id, new FrameConnectionReference(connection)))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void RemoveConnection(long id)
        {
            if (!_connectionReferences.TryRemove(id, out _))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void Walk(Action<FrameConnection> callback)
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
                }

                // If both conditions are false, the connection was removed during the heartbeat.
            }
        }

        private static ResourceCounter GetCounter(long? number)
            => number.HasValue
                ? ResourceCounter.Quota(number.Value)
                : ResourceCounter.Unlimited;
    }
}
