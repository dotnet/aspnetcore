// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public static class ConnectionManagerShutdownExtensions
    {
        public static async Task<bool> CloseAllConnectionsAsync(this ConnectionManager connectionManager, CancellationToken token)
        {
            var closeTasks = new List<Task>();

            connectionManager.Walk(connection =>
            {
                connection.TransportConnection.RequestClose();
                closeTasks.Add(connection.ExecutionTask);
            });

            var allClosedTask = Task.WhenAll(closeTasks.ToArray());
            return await Task.WhenAny(allClosedTask, CancellationTokenAsTask(token)).ConfigureAwait(false) == allClosedTask;
        }

        public static async Task<bool> AbortAllConnectionsAsync(this ConnectionManager connectionManager)
        {
            var abortTasks = new List<Task>();

            connectionManager.Walk(connection =>
            {
                connection.TransportConnection.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedDuringServerShutdown));
                abortTasks.Add(connection.ExecutionTask);
            });

            var allAbortedTask = Task.WhenAll(abortTasks.ToArray());
            return await Task.WhenAny(allAbortedTask, Task.Delay(1000)).ConfigureAwait(false) == allAbortedTask;
        }

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => tcs.SetResult(null));
            return tcs.Task;
        }
    }
}
