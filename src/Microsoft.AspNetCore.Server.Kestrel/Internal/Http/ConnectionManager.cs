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

        public async Task<bool> WalkConnectionsAndCloseAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();

            _thread.Post(state => ((ConnectionManager)state).WalkConnectionsAndCloseCore(tcs), this);

            return await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false) == tcs.Task;
        }

        private void WalkConnectionsAndCloseCore(TaskCompletionSource<object> tcs)
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
                tcs.SetResult(null);
            });
        }
    }
}
