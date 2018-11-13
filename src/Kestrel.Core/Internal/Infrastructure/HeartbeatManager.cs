// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class HeartbeatManager : IHeartbeatHandler, ISystemClock
    {
        private readonly ConnectionManager _connectionManager;
        private readonly Action<KestrelConnection> _walkCallback;
        private DateTimeOffset _now;
        private long _nowTicks;

        public HeartbeatManager(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            _walkCallback = WalkCallback;
        }

        public DateTimeOffset UtcNow => new DateTimeOffset(UtcNowTicks, TimeSpan.Zero);

        public long UtcNowTicks => Volatile.Read(ref _nowTicks);

        public DateTimeOffset UtcNowUnsynchronized => _now;

        public void OnHeartbeat(DateTimeOffset now)
        {
            _now = now;
            Volatile.Write(ref _nowTicks, now.Ticks);

            _connectionManager.Walk(_walkCallback);
        }

        private void WalkCallback(KestrelConnection connection)
        {
            connection.TransportConnection.TickHeartbeat();
        }
    }
}
