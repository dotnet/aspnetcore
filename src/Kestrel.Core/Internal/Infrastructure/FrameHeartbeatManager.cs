// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class FrameHeartbeatManager : IHeartbeatHandler
    {
        private readonly FrameConnectionManager _connectionManager;
        private readonly Action<FrameConnection> _walkCallback;
        private DateTimeOffset _now;

        public FrameHeartbeatManager(FrameConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            _walkCallback = WalkCallback;
        }

        public void OnHeartbeat(DateTimeOffset now)
        {
            _now = now;
            _connectionManager.Walk(_walkCallback);
        }

        private void WalkCallback(FrameConnection connection)
        {
            connection.Tick(_now);
        }
    }
}
