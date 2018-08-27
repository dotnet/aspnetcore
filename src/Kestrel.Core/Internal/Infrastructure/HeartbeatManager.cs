// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class HeartbeatManager : IHeartbeatHandler, ISystemClock
    {
        private readonly ConnectionManager _connectionManager;
        private readonly Action<KestrelConnection> _walkCallback;
        private DateTimeOffset _now;

        public HeartbeatManager(ConnectionManager connectionManager, DateTimeOffset initialUtcNow)
        {
            _connectionManager = connectionManager;
            _now = initialUtcNow;
            _walkCallback = WalkCallback;
        }

        public DateTimeOffset UtcNow => _now;

        public void OnHeartbeat(DateTimeOffset now)
        {
            _now = now;
            _connectionManager.Walk(_walkCallback);
        }

        private void WalkCallback(KestrelConnection connection)
        {
            connection.TransportConnection.TickHeartbeat();
        }
    }
}
