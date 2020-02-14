// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class MultiplexedHeartbeatManager : IHeartbeatHandler, ISystemClock
    {
        private readonly MultiplexedConnectionManager _connectionManager;
        private readonly Action<MultiplexedKestrelConnection> _walkCallback;
        private DateTimeOffset _now;
        private long _nowTicks;

        public MultiplexedHeartbeatManager(MultiplexedConnectionManager connectionManager)
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

        private void WalkCallback(MultiplexedKestrelConnection connection)
        {
            connection.TickHeartbeat();
        }
    }
}
