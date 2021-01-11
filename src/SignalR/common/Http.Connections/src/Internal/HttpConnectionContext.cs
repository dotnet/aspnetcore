// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal class HttpConnectionContext : ConnectionContext,
                                         IConnectionIdFeature,
                                         IConnectionItemsFeature,
                                         IConnectionTransportFeature,
                                         IConnectionUserFeature,
                                         IConnectionHeartbeatFeature,
                                         ITransferFormatFeature,
                                         IHttpContextFeature,
                                         IHttpTransportFeature,
                                         IConnectionInherentKeepAliveFeature,
                                         IConnectionLifetimeFeature
    {
        private static long _tenSeconds = TimeSpan.FromSeconds(10).Ticks;

        private readonly object _stateLock = new object();
        private readonly object _itemsLock = new object();
        private readonly object _heartbeatLock = new object();
        private List<(Action<object> handler, object state)>? _heartbeatHandlers;
        private readonly ILogger _logger;
        private PipeWriterStream _applicationStream;
        private IDuplexPipe _application;
        private IDictionary<object, object?>? _items;
        private CancellationTokenSource _connectionClosedTokenSource;

        private CancellationTokenSource? _sendCts;
        private bool _activeSend;
        private long _startedSendTime;
        private readonly object _sendingLock = new object();
        internal CancellationToken SendingToken { get; private set; }

        // This tcs exists so that multiple calls to DisposeAsync all wait asynchronously
        // on the same task
        private readonly TaskCompletionSource _disposeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Creates the DefaultConnectionContext without Pipes to avoid upfront allocations.
        /// The caller is expected to set the <see cref="Transport"/> and <see cref="Application"/> pipes manually.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="connectionToken"></param>
        /// <param name="logger"></param>
        /// <param name="transport"></param>
        /// <param name="application"></param>
        public HttpConnectionContext(string connectionId, string connectionToken, ILogger logger, IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            _applicationStream = new PipeWriterStream(application.Output);
            _application = application;

            ConnectionId = connectionId;
            ConnectionToken = connectionToken;
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
            Features.Set<IConnectionLifetimeFeature>(this);

            _connectionClosedTokenSource = new CancellationTokenSource();
            ConnectionClosed = _connectionClosedTokenSource.Token;
        }

        public CancellationTokenSource? Cancellation { get; set; }

        public HttpTransportType TransportType { get; set; }

        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);

        // Used for testing only
        internal Task? DisposeAndRemoveTask { get; set; }

        // Used for LongPolling because we need to create a scope that spans the lifetime of multiple requests on the cloned HttpContext
        internal IServiceScope? ServiceScope { get; set; }

        public Task? TransportTask { get; set; }

        public Task PreviousPollTask { get; set; } = Task.CompletedTask;

        public Task? ApplicationTask { get; set; }

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

        internal string ConnectionToken { get; set; }

        public override IFeatureCollection Features { get; }

        public ClaimsPrincipal? User { get; set; }

        public bool HasInherentKeepAlive { get; set; }

        public override IDictionary<object, object?> Items
        {
            get
            {
                if (_items == null)
                {
                    lock (_itemsLock)
                    {
                        if (_items == null)
                        {
                            _items = new ConnectionItems(new ConcurrentDictionary<object, object?>());
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
                _applicationStream = new PipeWriterStream(value.Output);
                _application = value;
            }
        }

        internal PipeWriterStream ApplicationStream => _applicationStream;

        public override IDuplexPipe Transport { get; set; }

        public TransferFormat SupportedFormats { get; set; }

        public TransferFormat ActiveFormat { get; set; }

        public HttpContext? HttpContext { get; set; }

        public override CancellationToken ConnectionClosed { get; set; }

        public override void Abort()
        {
            ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);

            HttpContext?.Abort();
        }

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

                ServiceScope?.Dispose();
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
                        // The websocket transport will close the application output automatically when reading is canceled
                        Cancellation?.Cancel();
                    }
                    else
                    {
                        // Normally it isn't safe to try and acquire this lock because the Send can hold onto it for a long time if there is backpressure
                        // It is safe to wait for this lock now because the Send will be in one of 4 states
                        // 1. In the middle of a write which is in the middle of being canceled by the CancelPendingFlush above, when it throws
                        //    an OperationCanceledException it will complete the PipeWriter which will make any other Send waiting on the lock
                        //    throw an InvalidOperationException if they call Write
                        // 2. About to write and see that there is a pending cancel from the CancelPendingFlush, go to 1 to see what happens
                        // 3. Enters the Send and sees the Dispose state from DisposeAndRemoveAsync and releases the lock
                        // 4. No Send in progress
                        await WriteLock.WaitAsync();
                        try
                        {
                            // Complete the applications read loop
                            Application?.Output.Complete();
                        }
                        finally
                        {
                            WriteLock.Release();
                        }

                        Application?.Input.CancelPendingRead();
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

                        // Trigger ConnectionClosed
                        ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);
                    }
                }
                else
                {
                    // If the transport is complete, complete the application pipes
                    Application?.Output.Complete(transportTask.Exception?.InnerException);
                    Application?.Input.Complete();

                    // Trigger ConnectionClosed
                    ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);

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
                _disposeTcs.TrySetResult();
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

        internal bool TryActivatePersistentConnection(
            ConnectionDelegate connectionDelegate,
            IHttpTransport transport,
            HttpContext context,
            ILogger dispatcherLogger)
        {
            lock (_stateLock)
            {
                if (Status == HttpConnectionStatus.Inactive)
                {
                    Status = HttpConnectionStatus.Active;

                    // Call into the end point passing the connection
                    ApplicationTask = ExecuteApplication(connectionDelegate);

                    // Start the transport
                    TransportTask = transport.ProcessRequestAsync(context, context.RequestAborted);

                    return true;
                }
                else
                {
                    FailActivationUnsynchronized(context, dispatcherLogger);

                    return false;
                }
            }
        }

        public bool TryActivateLongPollingConnection(
            ConnectionDelegate connectionDelegate,
            HttpContext nonClonedContext,
            TimeSpan pollTimeout,
            Task currentRequestTask,
            ILoggerFactory loggerFactory,
            ILogger dispatcherLogger)
        {
            lock (_stateLock)
            {
                if (Status == HttpConnectionStatus.Inactive)
                {
                    Status = HttpConnectionStatus.Active;

                    PreviousPollTask = currentRequestTask;

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (ApplicationTask == null)
                    {
                        HttpConnectionDispatcher.Log.EstablishedConnection(dispatcherLogger);

                        ApplicationTask = ExecuteApplication(connectionDelegate);

                        nonClonedContext.Response.ContentType = "application/octet-stream";

                        // This request has no content
                        nonClonedContext.Response.ContentLength = 0;

                        // On the first poll, we flush the response immediately to mark the poll as "initialized" so future
                        // requests can be made safely
                        TransportTask = nonClonedContext.Response.Body.FlushAsync();
                    }
                    else
                    {
                        HttpConnectionDispatcher.Log.ResumingConnection(dispatcherLogger);

                        // REVIEW: Performance of this isn't great as this does a bunch of per request allocations
                        Cancellation = new CancellationTokenSource();

                        var timeoutSource = new CancellationTokenSource();
                        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(Cancellation.Token, nonClonedContext.RequestAborted, timeoutSource.Token);

                        // Dispose these tokens when the request is over
                        nonClonedContext.Response.RegisterForDispose(timeoutSource);
                        nonClonedContext.Response.RegisterForDispose(tokenSource);

                        var longPolling = new LongPollingServerTransport(timeoutSource.Token, Application.Input, loggerFactory, this);

                        // Start the transport
                        TransportTask = longPolling.ProcessRequestAsync(nonClonedContext, tokenSource.Token);

                        // Start the timeout after we return from creating the transport task
                        timeoutSource.CancelAfter(pollTimeout);
                    }

                    return true;
                }
                else
                {
                    FailActivationUnsynchronized(nonClonedContext, dispatcherLogger);

                    return false;
                }
            }
        }

        private void FailActivationUnsynchronized(HttpContext nonClonedContext, ILogger dispatcherLogger)
        {
            if (Status == HttpConnectionStatus.Active)
            {
                HttpConnectionDispatcher.Log.ConnectionAlreadyActive(dispatcherLogger, ConnectionId, HttpContext!.TraceIdentifier);

                // Reject the request with a 409 conflict
                nonClonedContext.Response.StatusCode = StatusCodes.Status409Conflict;
                nonClonedContext.Response.ContentType = "text/plain";
            }
            else
            {
                Debug.Assert(Status == HttpConnectionStatus.Disposed);

                HttpConnectionDispatcher.Log.ConnectionDisposed(dispatcherLogger, ConnectionId);

                // Connection was disposed
                nonClonedContext.Response.StatusCode = StatusCodes.Status404NotFound;
                nonClonedContext.Response.ContentType = "text/plain";
            }
        }

        internal async Task<bool> CancelPreviousPoll(HttpContext context)
        {
            CancellationTokenSource? cts;
            lock (_stateLock)
            {
                // Need to sync cts access with DisposeAsync as that will dispose the cts
                if (Status == HttpConnectionStatus.Disposed)
                {
                    cts = null;
                }
                else
                {
                    cts = Cancellation;
                    Cancellation = null;
                }
            }

            using (cts)
            {
                // Cancel the previous request
                cts?.Cancel();

                try
                {
                    // Wait for the previous request to drain
                    await PreviousPollTask;
                }
                catch (OperationCanceledException)
                {
                    // Previous poll canceled due to connection closing, close this poll too
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return false;
                }

                return true;
            }
        }

        public void MarkInactive()
        {
            lock (_stateLock)
            {
                if (Status == HttpConnectionStatus.Active)
                {
                    Status = HttpConnectionStatus.Inactive;
                    LastSeenUtc = DateTime.UtcNow;
                }
            }
        }

        private async Task ExecuteApplication(ConnectionDelegate connectionDelegate)
        {
            // Verify some initialization invariants
            Debug.Assert(TransportType != HttpTransportType.None, "Transport has not been initialized yet");

            // Jump onto the thread pool thread so blocking user code doesn't block the setup of the
            // connection and transport
            await AwaitableThreadPool.Yield();

            // Running this in an async method turns sync exceptions into async ones
            await connectionDelegate(this);
        }

        internal void StartSendCancellation()
        {
            lock (_sendingLock)
            {
                if (_sendCts == null || _sendCts.IsCancellationRequested)
                {
                    _sendCts = new CancellationTokenSource();
                    SendingToken = _sendCts.Token;
                }
                _startedSendTime = DateTime.UtcNow.Ticks;
                _activeSend = true;
            }
        }
        internal void TryCancelSend(long currentTicks)
        {
            lock (_sendingLock)
            {
                if (_activeSend)
                {
                    if (currentTicks - _startedSendTime > _tenSeconds)
                    {
                        _sendCts!.Cancel();
                    }
                }
            }
        }
        internal void StopSendCancellation()
        {
            lock (_sendingLock)
            {
                _activeSend = false;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception?> _disposingConnection =
                LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1, "DisposingConnection"), "Disposing connection {TransportConnectionId}.");

            private static readonly Action<ILogger, Exception?> _waitingForApplication =
                LoggerMessage.Define(LogLevel.Trace, new EventId(2, "WaitingForApplication"), "Waiting for application to complete.");

            private static readonly Action<ILogger, Exception?> _applicationComplete =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "ApplicationComplete"), "Application complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _waitingForTransport =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(4, "WaitingForTransport"), "Waiting for {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _transportComplete =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(5, "TransportComplete"), "{TransportType} transport complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _shuttingDownTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(6, "ShuttingDownTransportAndApplication"), "Shutting down both the application and the {TransportType} transport.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _waitingForTransportAndApplication =
                LoggerMessage.Define<HttpTransportType>(LogLevel.Trace, new EventId(7, "WaitingForTransportAndApplication"), "Waiting for both the application and {TransportType} transport to complete.");

            private static readonly Action<ILogger, HttpTransportType, Exception?> _transportAndApplicationComplete =
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
