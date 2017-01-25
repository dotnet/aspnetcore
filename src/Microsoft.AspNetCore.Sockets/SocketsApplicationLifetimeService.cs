// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketsApplicationLifetimeService : IHostedService
    {
        private readonly ConnectionManager _connectionManager;

        public SocketsApplicationLifetimeService(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public void Start()
        {
            _connectionManager.Start();
        }

        public void Stop()
        {
            _connectionManager.CloseConnections();
        }
    }
}
