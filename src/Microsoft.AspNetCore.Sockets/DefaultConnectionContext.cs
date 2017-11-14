// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Sockets
{
    public class DefaultConnectionContext : ConnectionContext,
                                            IConnectionIdFeature,
                                            IConnectionMetadataFeature,
                                            IConnectionTransportFeature,
                                            IConnectionUserFeature,
                                            ITransferModeFeature
    {
        // This tcs exists so that multiple calls to DisposeAsync all wait asynchronously
        // on the same task
        private TaskCompletionSource<object> _disposeTcs = new TaskCompletionSource<object>();
        internal ValueStopwatch ConnectionTimer { get; set; }

        public DefaultConnectionContext(string id, Channel<byte[]> transport, Channel<byte[]> application)
        {
            Transport = transport;
            Application = application;
            ConnectionId = id;
            LastSeenUtc = DateTime.UtcNow;

            // PERF: This type could just implement IFeatureCollection
            Features = new FeatureCollection();
            Features.Set<IConnectionUserFeature>(this);
            Features.Set<IConnectionMetadataFeature>(this);
            Features.Set<IConnectionIdFeature>(this);
            Features.Set<IConnectionTransportFeature>(this);
            Features.Set<ITransferModeFeature>(this);
        }

        public CancellationTokenSource Cancellation { get; set; }

        public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);

        public Task TransportTask { get; set; }

        public Task ApplicationTask { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Inactive;

        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; }

        public ClaimsPrincipal User { get; set; }

        public override IDictionary<object, object> Metadata { get; set; } = new ConnectionMetadata();

        public Channel<byte[]> Application { get; }

        public override Channel<byte[]> Transport { get; set; }

        public TransferMode TransportCapabilities { get; set; }

        public TransferMode TransferMode { get; set; }

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

                    // If the application task is faulted, propagate the error to the transport
                    if (ApplicationTask?.IsFaulted == true)
                    {
                        Transport.Writer.TryComplete(ApplicationTask.Exception.InnerException);
                    }
                    else
                    {
                        Transport.Writer.TryComplete();
                    }

                    // If the transport task is faulted, propagate the error to the application
                    if (TransportTask?.IsFaulted == true)
                    {
                        Application.Writer.TryComplete(TransportTask.Exception.InnerException);
                    }
                    else
                    {
                        Application.Writer.TryComplete();
                    }

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
