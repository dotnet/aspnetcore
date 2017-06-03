// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Sockets
{
    public class DefaultConnectionContext : ConnectionContext
    {
        // This tcs exists so that multiple calls to DisposeAsync all wait asynchronously
        // on the same task
        private TaskCompletionSource<object> _disposeTcs = new TaskCompletionSource<object>();

        public DefaultConnectionContext(string id, IChannelConnection<Message> transport, IChannelConnection<Message> application)
        {
            Transport = transport;
            Application = application;
            ConnectionId = id;
        }

        public CancellationTokenSource Cancellation { get; set; }

        public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

        // REVIEW: This should only be on the Http implementation
        public string RequestId { get; set; }

        public Task TransportTask { get; set; }

        public Task ApplicationTask { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Inactive;

        public override string ConnectionId { get; }

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override ClaimsPrincipal User { get; set; }

        public override ConnectionMetadata Metadata { get; } = new ConnectionMetadata();

        public IChannelConnection<Message> Application { get; }

        public override IChannelConnection<Message> Transport { get; set; }

        public async Task DisposeAsync()
        {
            Task disposeTask = Task.CompletedTask;

            try
            {
                await Lock.WaitAsync();

                if (Status == ConnectionStatus.Disposed)
                {
                    disposeTask = _disposeTcs.Task;
                }
                else
                {
                    Status = ConnectionStatus.Disposed;

                    RequestId = null;

                    // If the application task is faulted, propagate the error to the transport
                    if (ApplicationTask?.IsFaulted == true)
                    {
                        Transport.Output.TryComplete(ApplicationTask.Exception.InnerException);
                    }

                    // If the transport task is faulted, propagate the error to the application
                    if (TransportTask?.IsFaulted == true)
                    {
                        Application.Output.TryComplete(TransportTask.Exception.InnerException);
                    }

                    Transport.Dispose();
                    Application.Dispose();

                    var applicationTask = ApplicationTask ?? Task.CompletedTask;
                    var transportTask = TransportTask ?? Task.CompletedTask;

                    disposeTask = WaitOnTasks(applicationTask, transportTask);
                }
            }
            finally
            {
                Lock.Release();
            }

            await disposeTask;
        }

        private async Task WaitOnTasks(Task applicationTask, Task transportTask)
        {
            try
            {
                await Task.WhenAll(applicationTask, transportTask);

                // Notify all waiters that we're done disposing
                _disposeTcs.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                _disposeTcs.TrySetCanceled();

                throw;
            }
            catch (Exception ex)
            {
                _disposeTcs.TrySetException(ex);

                throw;
            }
        }


        public enum ConnectionStatus
        {
            Inactive,
            Active,
            Disposed
        }
    }
}
