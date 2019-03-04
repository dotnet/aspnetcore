// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public class HttpConnectionContext : ConnectionContext,
                                         IConnectionIdFeature,
                                         IConnectionItemsFeature,
                                         IConnectionTransportFeature,
                                         IConnectionUserFeature,
                                         IConnectionHeartbeatFeature,
                                         ITransferFormatFeature,
                                         IHttpContextFeature,
                                         IHttpTransportFeature,
                                         IConnectionInherentKeepAliveFeature
    {
        private readonly object _stateLock = new object();
        private readonly object _itemsLock = new object();
        private readonly object _heartbeatLock = new object();
        private List<(Action<object> handler, object state)> _heartbeatHandlers;
        private readonly ILogger _logger;
        private PipeWriterStream _applicationStream;
        private IDuplexPipe _application;
        private IDictionary<object, object> _items;

        // This tcs exists so that multiple calls to DisposeAsync all wait asynchronously
        // on the same task
        private readonly TaskCompletionSource<object> _disposeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Creates the DefaultConnectionContext without Pipes to avoid upfront allocations.
        /// The caller is expected to set the <see cref="Transport"/> and <see cref="Application"/> pipes manually.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="logger"></param>
        public HttpConnectionContext(string id, ILogger logger)
        {
            ConnectionId = id;
            LastSeenUtc = DateTime.UtcNow;

            // The default behavior is that both formats are supported.
            SupportedFormats = TransferFormat.Binary | TransferFormat.Text;
            ActiveFormat = TransferFormat.Text;

            _logger = logger;

            // PERF: This type could just implement IFeatureCollection
            Features = new FeatureCollection();
            Features.Set<IConnectionUserFeature>(this);
            Features.Set<IConnectionItemsFeature>(this);
            Features.Set<IConnectionIdFeature>(this);
            Features.Set<IConnectionTransportFeature>(this);
            Features.Set<IConnectionHeartbeatFeature>(this);
            Features.Set<ITransferFormatFeature>(this);
            Features.Set<IHttpContextFeature>(this);
            Features.Set<IHttpTransportFeature>(this);
            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        public HttpConnectionContext(string id, IDuplexPipe transport, IDuplexPipe application, ILogger logger = null)
            : this(id, logger)
        {
            Transport = transport;
            Application = application;
        }

        public CancellationTokenSource Cancellation { get; set; }

        public HttpTransportType TransportType { get; set; }

        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);

        // Used for testing only
        internal Task DisposeAndRemoveTask { get; set; }

        public Task TransportTask { get; set; }

        public Task PreviousPollTask { get; set; } = Task.CompletedTask;

        public Task ApplicationTask { get; set; }

        public DateTime LastSeenUtc { get; set; }

        public DateTime? LastSeenUtcIfInactive
        {
            get
            {
                lock (_stateLock)
                {
                    return Status == HttpConnectionStatus.Inactive ? (DateTime?)LastSeenUtc : null;
                }
            }
        }

        public HttpConnectionStatus Status { get; set; } = HttpConnectionStatus.Inactive;

        public override string ConnectionId { get; set; }

        public override IFeatureCollection Features { get; }

        public ClaimsPrincipal User { get; set; }

        public bool HasInherentKeepAlive { get; set; }

        public override IDictionary<object, object> Items
        {
            get
            {
                if (_items == null)
                {
                    lock (_itemsLock)
                    {
                        if (_items == null)
                        {
                            _items = new ConnectionItems(new ConcurrentDictionary<object, object>());
                        }
                    }
                }
                return _items;
            }
            set => _items = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IDuplexPipe Application
        {
            get => _application;
            set
            {
                if (value != null)
                {
                    _applicationStream = new PipeWriterStream(value.Output);
                }
                else
                {
                    _applicationStream = null;
                }
                _application = value;
            }
        }

        internal PipeWriterStream ApplicationStream => _applicationStream;

        public override IDuplexPipe Transport { get; set; }

        public TransferFormat SupportedFormats { get; set; }

        public TransferFormat ActiveFormat { get; set; }

        public HttpContext HttpContext { get; set; }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }
                _heartbeatHandlers.Add((action, state));
            }
        }

        public void TickHeartbeat()
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    return;
                }

                foreach (var (handler, state) in _heartbeatHandlers)
                {
                    handler(state);
                }
            }
        }

        public async Task DisposeAsync(bool closeGracefully = false)
        {
            Task disposeTask;

            try
            {
                lock (_stateLock)
                {
                    if (Status == HttpConnectionStatus.Disposed)
                    {
                        disposeTask = _disposeTcs.Task;
                    }
                    else
                    {
                        Status = HttpConnectionStatus.Disposed;

                        Log.DisposingConnection(_logger, ConnectionId);

                        var applicationTask = ApplicationTask ?? Task.CompletedTask;
                        var transportTask = TransportTask ?? Task.CompletedTask;

                        disposeTask = WaitOnTasks(applicationTask, transportTask, closeGracefully);
                    }
                }
            }
            finally
            {
                Cancellation?.Dispose();

                Cancellation = null;

                if (User != null && User.Identity is WindowsIdentity)
                {
                    foreach (var identity in User.Identities)
                    {
                        (identity as IDisposable)?.Dispose();
                    }
                }
            }

            await disposeTask;
        }

        private async Task WaitOnTasks(Task applicationTask, Task transportTask, bool closeGracefully)
        {
            try
            {
                // Closing gracefully means we're only going to close the finished sides of the pipe
                // If the application finishes, that means it's done with the transport pipe
                // If the transport finishes, that means it's done with the application pipe
                if (!closeGracefully)
                {
                    Application?.Output.CancelPendingFlush();

                    if (TransportType == HttpTransportType.WebSockets)
                    {
                        // The websocket transport will close the application output automatically when reading is cancelled
                        Cancellation?.Cancel();
                    }
                    else
                    {
                        // The other transports don't close their own output, so we can do it here safely
                        Application?.Output.Complete();
                    }
                }

                // Wait for either to finish
                var result = await Task.WhenAny(applicationTask, transportTask);

                // If the application is complete, complete the transport pipe (it's the pipe to the transport)
                if (result == applicationTask)
                {
                    Transport?.Output.Complete(applicationTask.Exception?.InnerException);
                    Transport?.Input.Complete();

                    try
                    {
                        Log.WaitingForTransport(_logger, TransportType);

                        // Transports are written by us and are well behaved, wait for them to drain
                        await transportTask;
                    }
                    finally
                    {
                        Log.TransportComplete(_logger, TransportType);

                        // Now complete the application
                        Application?.Output.Complete();
                        Application?.Input.Complete();
                    }
                }
                else
                {
                    // If the transport is complete, complete the application pipes
                    Application?.Output.Complete(transportTask.Exception?.InnerException);
                    Application?.Input.Complete();

                    try
                    {
                        // A poorly written application *could* in theory get stuck forever and it'll show up as a memory leak
                        Log.WaitingForApplication(_logger);

                        await applicationTask;
                    }
                    finally
                    {
                        Log.ApplicationComplete(_logger);

                        Transport?.Output.Complete();
                        Transport?.Input.Complete();
                    }
                }

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

        public HttpConnectionStatus TryChangeState(HttpConnectionStatus from, HttpConnectionStatus to)
        {
            lock (_stateLock)
            {
                if (Status == from)
                {
                    Status = to;
                    return from;
                }

                return Status;
            }
        }

        public void MarkInactive()
        {
            lock (_stateLock)
            {
                LastSeenUtc = DateTime.UtcNow;

                if (Status == HttpConnectionStatus.Active)
                {
                    Status = HttpConnectionStatus.Inactive;
                }
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> _disposingConnection =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1, "DisposingConnection"), "Disposing connection {TransportConnectionId}.");

            private static readonly Action<ILogger, Exception> _waitingForApplication =
                LoggerMessage.Define(LogLevel.Trace, new EventId(2, "WaitingForApplication"), "Waiting for application to complete.");

            private static readonly Action<ILogger, Exception> _applicationComplete =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "ApplicationComplete"), "Application complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _waitingForTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(4, "WaitingForTransport"), "Waiting for {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportComplete =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(5, "TransportComplete"), "{TransportType} transport complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _shuttingDownTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(6, "ShuttingDownTransportAndApplication"), "Shutting down both the application and the {TransportType} transport.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _waitingForTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(7, "WaitingForTransportAndApplication"), "Waiting for both the application and {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception> _transportAndApplicationComplete =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(8, "TransportAndApplicationComplete"), "The application and {TransportType} transport are both complete.");

            public static void DisposingConnection(ILogger logger, string connectionId)
            {
                if (logger == null)
                {
                    return;
                }

                _disposingConnection(logger, connectionId, null);
            }

            public static void WaitingForApplication(ILogger logger)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForApplication(logger, null);
            }

            public static void ApplicationComplete(ILogger logger)
            {
                if (logger == null)
                {
                    return;
                }

                _applicationComplete(logger, null);
            }

            public static void WaitingForTransport(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForTransport(logger, transportType, null);
            }

            public static void TransportComplete(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _transportComplete(logger, transportType, null);
            }
            public static void ShuttingDownTransportAndApplication(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _shuttingDownTransportAndApplication(logger, transportType, null);
            }

            public static void WaitingForTransportAndApplication(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _waitingForTransportAndApplication(logger, transportType, null);
            }

            public static void TransportAndApplicationComplete(ILogger logger, HttpTransportType transportType)
            {
                if (logger == null)
                {
                    return;
                }

                _transportAndApplicationComplete(logger, transportType, null);
            }
        }
    }
}
