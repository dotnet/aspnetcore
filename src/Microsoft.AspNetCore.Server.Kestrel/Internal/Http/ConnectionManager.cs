// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class ConnectionManager
    {
        private readonly KestrelThread _thread;
        private readonly IThreadPool _threadPool;

        public ConnectionManager(KestrelThread thread, IThreadPool threadPool)
        {
            _thread = thread;
            _threadPool = threadPool;
        }

        public bool WalkConnectionsAndClose(TimeSpan timeout)
        {
            var wh = new ManualResetEventSlim();

            _thread.Post(state => ((ConnectionManager)state).WalkConnectionsAndCloseCore(wh), this);

            return wh.Wait(timeout);
        }

        private void WalkConnectionsAndCloseCore(ManualResetEventSlim wh)
        {
            var connectionStopTasks = new List<Task>();

            _thread.Walk(ptr =>
            {
                var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                var connection = (handle as UvStreamHandle)?.Connection;

                if (connection != null)
                {
                    connectionStopTasks.Add(connection.StopAsync());
                }
            });

            _threadPool.Run(() =>
            {
                Task.WaitAll(connectionStopTasks.ToArray());
                wh.Set();
            });
        }
    }
}
