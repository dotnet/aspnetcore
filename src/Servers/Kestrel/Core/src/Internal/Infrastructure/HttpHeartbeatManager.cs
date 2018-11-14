// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class HttpHeartbeatManager : IHeartbeatHandler
    {
        private readonly HttpConnectionManager _connectionManager;
        private readonly Action<HttpConnection> _walkCallback;
        private DateTimeOffset _now;

        public HttpHeartbeatManager(HttpConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            _walkCallback = WalkCallback;
        }

        public void OnHeartbeat(DateTimeOffset now)
        {
            _now = now;
            _connectionManager.Walk(_walkCallback);
        }

        private void WalkCallback(HttpConnection connection)
        {
            connection.Tick(_now);
        }
    }
}
