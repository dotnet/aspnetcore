// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        }

        public void Stop()
        {
            _connectionManager.CloseConnections();
        }
    }
}
