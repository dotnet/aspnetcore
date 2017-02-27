// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class ConnectionState
    {
        public Connection Connection { get; set; }
        public IChannelConnection<Message> Application { get; }

        public CancellationTokenSource Cancellation { get; set; }

        public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

        public string RequestId { get; set; }

        public Task TransportTask { get; set; }
        public Task ApplicationTask { get; set; }

        public DateTime LastSeenUtc { get; set; }
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Inactive;

        public ConnectionState(Connection connection, IChannelConnection<Message> application)
        {
            Connection = connection;
            Application = application;
            LastSeenUtc = DateTime.UtcNow;
        }

        public async Task DisposeAsync()
        {
            Task applicationTask = TaskCache.CompletedTask;
            Task transportTask = TaskCache.CompletedTask;

            try
            {
                await Lock.WaitAsync();

                if (Status == ConnectionStatus.Disposed)
                {
                    return;
                }

                Status = ConnectionStatus.Disposed;

                RequestId = null;

                // If the application task is faulted, propagate the error to the transport
                if (ApplicationTask?.IsFaulted == true)
                {
                    Connection.Transport.Output.TryComplete(ApplicationTask.Exception.InnerException);
                }

                // If the transport task is faulted, propagate the error to the application
                if (TransportTask?.IsFaulted == true)
                {
                    Application.Output.TryComplete(TransportTask.Exception.InnerException);
                }

                Connection.Dispose();
                Application.Dispose();

                applicationTask = ApplicationTask ?? applicationTask;
                transportTask = TransportTask ?? transportTask;
            }
            finally
            {
                Lock.Release();
            }

            // REVIEW: Add a timeout so we don't wait forever
            await Task.WhenAll(applicationTask, transportTask);
        }

        public enum ConnectionStatus
        {
            Inactive,
            Active,
            Disposed
        }
    }
}
