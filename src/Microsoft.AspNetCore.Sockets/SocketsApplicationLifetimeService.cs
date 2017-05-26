// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketsApplicationLifetimeService : IHostedService
    {
        private readonly ConnectionManager _connectionManager;

        public SocketsApplicationLifetimeService(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public Task StartAsync(CancellationToken token)
        {
            _connectionManager.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            _connectionManager.CloseConnections();
            return Task.CompletedTask;
        }
    }
}
