// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class FrameConnectionManager : IHeartbeatHandler
    {
        private readonly ConcurrentDictionary<long, FrameConnection> _connections
            = new ConcurrentDictionary<long, FrameConnection>();

        public void AddConnection(long id, FrameConnection connection)
        {
            if (!_connections.TryAdd(id, connection))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void RemoveConnection(long id)
        {
            if (!_connections.TryRemove(id, out _))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void OnHeartbeat(DateTimeOffset now)
        {
            foreach (var kvp in _connections)
            {
                kvp.Value.Tick(now);
            }
        }
    }
}
