// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class ConnectionManager
    {
        private KestrelThread _thread;
        private List<Task> _connectionStopTasks;

        public ConnectionManager(KestrelThread thread)
        {
            _thread = thread;
        }

        // This must be called on the libuv event loop
        public void WalkConnectionsAndClose()
        {
            if (_connectionStopTasks != null)
            {
                throw new InvalidOperationException($"{nameof(WalkConnectionsAndClose)} cannot be called twice.");
            }

            _connectionStopTasks = new List<Task>();

            _thread.Walk(ptr =>
            {
                var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                var connection = (handle as UvStreamHandle)?.Connection;

                if (connection != null)
                {
                    _connectionStopTasks.Add(connection.StopAsync());
                }
            });
        }

        public Task WaitForConnectionCloseAsync()
        {
            if (_connectionStopTasks == null)
            {
                throw new InvalidOperationException($"{nameof(WalkConnectionsAndClose)} must be called first.");
            }

            return Task.WhenAll(_connectionStopTasks);
        }
    }
}
