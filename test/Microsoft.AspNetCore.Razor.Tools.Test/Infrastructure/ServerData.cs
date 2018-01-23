// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal sealed class ServerData : IDisposable
    {
        internal CancellationTokenSource CancellationTokenSource { get; }
        internal Task<ServerStats> ServerTask { get; }
        internal Task ListenTask { get; }
        internal string PipeName { get; }

        internal ServerData(CancellationTokenSource cancellationTokenSource, string pipeName, Task<ServerStats> serverTask, Task listenTask)
        {
            CancellationTokenSource = cancellationTokenSource;
            PipeName = pipeName;
            ServerTask = serverTask;
            ListenTask = listenTask;
        }

        internal async Task<ServerStats> CancelAndCompleteAsync()
        {
            CancellationTokenSource.Cancel();
            return await ServerTask;
        }

        internal async Task Verify(int connections, int completed)
        {
            var stats = await CancelAndCompleteAsync().ConfigureAwait(false);
            Assert.Equal(connections, stats.Connections);
            Assert.Equal(completed, stats.CompletedConnections);
        }

        public void Dispose()
        {
            if (!CancellationTokenSource.IsCancellationRequested)
            {
                CancellationTokenSource.Cancel();
            }

            ServerTask.Wait();
        }
    }
}
