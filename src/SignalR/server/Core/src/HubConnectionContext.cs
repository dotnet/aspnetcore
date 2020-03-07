// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Encapsulates all information about an individual connection to a SignalR Hub.
    /// </summary>
    public class HubConnectionContext
    {
        private static readonly Action<object> _cancelReader = state => ((PipeReader)state).CancelPendingRead();
        private static readonly WaitCallback _abortedCallback = AbortConnection;

        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly long _keepAliveInterval;
        private readonly long _clientTimeoutInterval;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        private readonly bool _useAbsoluteClientTimeout;
        private readonly object _receiveMessageTimeoutLock = new object();

        private StreamTracker _streamTracker;
        private long _lastSendTimeStamp = DateTime.UtcNow.Ticks;
        private long _lastReceivedTimeStamp = DateTime.UtcNow.Ticks;
        private bool _receivedMessageThisInterval = false;
        private ReadOnlyMemory<byte> _cachedPingMessage;
        private bool _clientTimeoutActive;
        private volatile bool _connectionAborted;
        private volatile bool _allowReconnect = true;
        private int _streamBufferCapacity;
        private long? _maxMessageSize;
        private bool _receivedMessageTimeoutEnabled = false;
        private long _receivedMessageElapsedTicks = 0;
        private long _receivedMessageTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnectionContext"/> class.
        /// </summary>
        /// <param name="connectionContext">The underlying <see cref="ConnectionContext"/>.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="contextOptions">The options to configure the HubConnectionContext.</param>
        public HubConnectionContext(ConnectionContext connectionContext, HubConnectionContextOptions contextOptions, ILoggerFactory loggerFactory)
        {
            _keepAliveInterval = contextOptions.KeepAliveInterval.Ticks;
            _clientTimeoutInterval = contextOptions.ClientTimeoutInterval.Ticks;
            _streamBufferCapacity = contextOptions.StreamBufferCapacity;
            _maxMessageSize = contextOptions.MaximumReceiveMessageSize;

            _connectionContext = connectionContext;
            _logger = loggerFactory.CreateLogger<HubConnectionContext>();
            ConnectionAborted = _connectionAbortedTokenSource.Token;

            HubCallerContext = new DefaultHubCallerContext(this);

            if (AppContext.TryGetSwitch("Microsoft.AspNetCore.SignalR.UseAbsoluteClientTimeout", out var useAbsoluteClientTimeout))
            {
                _useAbsoluteClientTimeout = useAbsoluteClientTimeout;
            }
        }

        internal StreamTracker StreamTracker
        {
            get
            {
                // lazy for performance reasons
                if (_streamTracker == null)
                {
                    _streamTracker = new StreamTracker(_streamBufferCapacity);
                }

                return _streamTracker;
            }
        }

        internal HubCallerContext HubCallerContext { get; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that notifies when the connection is aborted.
        /// </summary>
        public virtual CancellationToken ConnectionAborted { get; }

        /// <summary>
        /// Gets the ID for this connection.
        /// </summary>
        public virtual string ConnectionId => _connectionContext.ConnectionId;

        /// <summary>
        /// Gets the user for this connection.
        /// </summary>
        public virtual ClaimsPrincipal User => Features.Get<IConnectionUserFeature>()?.User;

        /// <summary>
        /// Gets the collection of features available on this connection.
        /// </summary>
        public virtual IFeatureCollection Features => _connectionContext.Features;

        /// <summary>
        /// Gets a key/value collection that can be used to share data within the scope of this connection.
        /// </summary>
        public virtual IDictionary<object, object> Items => _connectionContext.Items;

        // Used by HubConnectionHandler to determine whether to set CloseMessage.AllowReconnect.
        internal bool AllowReconnect => _allowReconnect;

        // Used by HubConnectionHandler
        internal PipeReader Input => _connectionContext.Transport.Input;

        /// <summary>
        /// Gets or sets the user identifier for this connection.
        /// </summary>
        public string UserIdentifier { get; set; }

        /// <summary>
        /// Gets the protocol used by this connection.
        /// </summary>
        public virtual IHubProtocol Protocol { get; set; }

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.Ordinal);

        public virtual ValueTask WriteAsync(HubMessage message, CancellationToken cancellationToken = default)
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message, cancellationToken));
            }

            if (_connectionAborted)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            // Otherwise, release the lock acquired when entering WriteAsync
            _writeLock.Release();

            return default;
        }

        /// <summary>
        /// This method is designed to support the framework and is not intended to be used by application code. Writes a pre-serialized message to the
        /// connection.
        /// </summary>
        /// <param name="message">The serialization cache to use.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns></returns>
        public virtual ValueTask WriteAsync(SerializedHubMessage message, CancellationToken cancellationToken = default)
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message, cancellationToken));
            }

            if (_connectionAborted)
            {
                _writeLock.Release();
                return default;
            }

            // This method should never throw synchronously
            var task = WriteCore(message, cancellationToken);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            // Otherwise, release the lock acquired when entering WriteAsync
            _writeLock.Release();

            return default;
        }

        private ValueTask<FlushResult> WriteCore(HubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                Protocol.WriteMessage(message, _connectionContext.Transport.Output);

                return _connectionContext.Transport.Output.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();

                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private ValueTask<FlushResult> WriteCore(SerializedHubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // Grab a preserialized buffer for this protocol.
                var buffer = message.GetSerializedMessage(Protocol);

                return _connectionContext.Transport.Output.WriteAsync(buffer, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();

                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private async Task CompleteWriteAsync(ValueTask<FlushResult> task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);

                AbortAllowReconnect();
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private async Task WriteSlowAsync(HubMessage message, CancellationToken cancellationToken)
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync(cancellationToken);

            try
            {
                if (_connectionAborted)
                {
                    return;
                }

                await WriteCore(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task WriteSlowAsync(SerializedHubMessage message, CancellationToken cancellationToken)
        {
            // Failed to get the lock immediately when entering WriteAsync so await until it is available
            await _writeLock.WaitAsync();

            try
            {
                if (_connectionAborted)
                {
                    return;
                }

                await WriteCore(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private ValueTask TryWritePingAsync()
        {
            // Don't wait for the lock, if it returns false that means someone wrote to the connection
            // and we don't need to send a ping anymore
            if (!_writeLock.Wait(0))
            {
                return default;
            }

            // TODO: cancel?
            return new ValueTask(TryWritePingSlowAsync());
        }

        private async Task TryWritePingSlowAsync()
        {
            try
            {
                if (_connectionAborted)
                {
                    return;
                }

                await _connectionContext.Transport.Output.WriteAsync(_cachedPingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
                AbortAllowReconnect();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task WriteHandshakeResponseAsync(HandshakeResponseMessage message)
        {
            await _writeLock.WaitAsync();

            try
            {
                if (message.Error == null)
                {
                    _connectionContext.Transport.Output.Write(HandshakeProtocol.GetSuccessfulHandshake(Protocol));
                }
                else
                {
                    HandshakeProtocol.WriteResponseMessage(message, _connectionContext.Transport.Output);
                }

                await _connectionContext.Transport.Output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Aborts the connection.
        /// </summary>
        public virtual void Abort()
        {
            _allowReconnect = false;
            AbortAllowReconnect();
        }

        private void AbortAllowReconnect()
        {
            _connectionAborted = true;
            // Cancel any current writes or writes that are about to happen and have already gone past the _connectionAborted bool
            // We have to do this outside of the lock otherwise it could hang if the write is observing backpressure
            _connectionContext.Transport.Output.CancelPendingFlush();

            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            Input.CancelPendingRead();

            // We fire and forget since this can trigger user code to run
            ThreadPool.QueueUserWorkItem(_abortedCallback, this);
        }

        internal async Task<bool> HandshakeAsync(TimeSpan timeout, IReadOnlyList<string> supportedProtocols, IHubProtocolResolver protocolResolver,
            IUserIdProvider userIdProvider, bool enableDetailedErrors)
        {
            try
            {
                var input = Input;

                using (var cts = new CancellationTokenSource())
                using (var registration = cts.Token.UnsafeRegister(_cancelReader, input))
                {
                    if (!Debugger.IsAttached)
                    {
                        cts.CancelAfter(timeout);
                    }

                    while (true)
                    {
                        var result = await input.ReadAsync();

                        var buffer = result.Buffer;
                        var consumed = buffer.Start;
                        var examined = buffer.End;

                        try
                        {
                            if (result.IsCanceled)
                            {
                                Log.HandshakeCanceled(_logger);
                                await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                                return false;
                            }

                            if (!buffer.IsEmpty)
                            {
                                var segment = buffer;
                                var overLength = false;

                                if (_maxMessageSize != null && buffer.Length > _maxMessageSize.Value)
                                {
                                    // We give the parser a sliding window of the default message size
                                    segment = segment.Slice(segment.Start, _maxMessageSize.Value);
                                    overLength = true;
                                }

                                if (HandshakeProtocol.TryParseRequestMessage(ref segment, out var handshakeRequestMessage))
                                {
                                    // We parsed the handshake
                                    consumed = segment.Start;
                                    examined = consumed;

                                    Protocol = protocolResolver.GetProtocol(handshakeRequestMessage.Protocol, supportedProtocols);
                                    if (Protocol == null)
                                    {
                                        Log.HandshakeFailed(_logger, null);

                                        await WriteHandshakeResponseAsync(new HandshakeResponseMessage($"The protocol '{handshakeRequestMessage.Protocol}' is not supported."));
                                        return false;
                                    }

                                    if (!Protocol.IsVersionSupported(handshakeRequestMessage.Version))
                                    {
                                        Log.ProtocolVersionFailed(_logger, handshakeRequestMessage.Protocol, handshakeRequestMessage.Version);
                                        await WriteHandshakeResponseAsync(new HandshakeResponseMessage(
                                            $"The server does not support version {handshakeRequestMessage.Version} of the '{handshakeRequestMessage.Protocol}' protocol."));
                                        return false;
                                    }

                                    // If there's a transfer format feature, we need to check if we're compatible and set the active format.
                                    // If there isn't a feature, it means that the transport supports binary data and doesn't need us to tell them
                                    // what format we're writing.
                                    var transferFormatFeature = Features.Get<ITransferFormatFeature>();
                                    if (transferFormatFeature != null)
                                    {
                                        if ((transferFormatFeature.SupportedFormats & Protocol.TransferFormat) == 0)
                                        {
                                            Log.HandshakeFailed(_logger, null);
                                            await WriteHandshakeResponseAsync(new HandshakeResponseMessage($"Cannot use the '{Protocol.Name}' protocol on the current transport. The transport does not support '{Protocol.TransferFormat}' transfer format."));
                                            return false;
                                        }

                                        transferFormatFeature.ActiveFormat = Protocol.TransferFormat;
                                    }

                                    _cachedPingMessage = Protocol.GetMessageBytes(PingMessage.Instance);

                                    UserIdentifier = userIdProvider.GetUserId(this);

                                    // != true needed because it could be null (which we treat as false)
                                    if (Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
                                    {
                                        // Only register KeepAlive after protocol handshake otherwise KeepAliveTick could try to write without having a ProtocolReaderWriter
                                        Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).KeepAliveTick(), this);
                                    }

                                    Log.HandshakeComplete(_logger, Protocol.Name);

                                    await WriteHandshakeResponseAsync(HandshakeResponseMessage.Empty);
                                    return true;
                                }
                                else if (overLength)
                                {
                                    Log.HandshakeSizeLimitExceeded(_logger, _maxMessageSize.Value);
                                    await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                                    return false;
                                }
                            }

                            if (result.IsCompleted)
                            {
                                // connection was closed before we ever received a response
                                // can't send a handshake response because there is no longer a connection
                                Log.HandshakeFailed(_logger, null);
                                return false;
                            }
                        }
                        finally
                        {
                            input.AdvanceTo(consumed, examined);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.HandshakeCanceled(_logger);
                await WriteHandshakeResponseAsync(new HandshakeResponseMessage("Handshake was canceled."));
                return false;
            }
            catch (Exception ex)
            {
                Log.HandshakeFailed(_logger, ex);
                var errorMessage = ErrorMessageHelper.BuildErrorMessage("An unexpected error occurred during connection handshake.", ex, enableDetailedErrors);
                await WriteHandshakeResponseAsync(new HandshakeResponseMessage(errorMessage));
                return false;
            }
        }

        // Used by the HubConnectionHandler only
        internal Task AbortAsync()
        {
            AbortAllowReconnect();

            // Acquire lock to make sure all writes are completed
            if (!_writeLock.Wait(0))
            {
                return AbortAsyncSlow();
            }
            _writeLock.Release();
            return _abortCompletedTcs.Task;
        }

        private async Task AbortAsyncSlow()
        {
            await _writeLock.WaitAsync();
            _writeLock.Release();
            await _abortCompletedTcs.Task;
        }

        private void KeepAliveTick()
        {
            var currentTime = DateTime.UtcNow.Ticks;

            // Implements the keep-alive tick behavior
            // Each tick, we check if the time since the last send is larger than the keep alive duration (in ticks).
            // If it is, we send a ping frame, if not, we no-op on this tick. This means that in the worst case, the
            // true "ping rate" of the server could be (_hubOptions.KeepAliveInterval + HubEndPoint.KeepAliveTimerInterval),
            // because if the interval elapses right after the last tick of this timer, it won't be detected until the next tick.

            if (currentTime - Volatile.Read(ref _lastSendTimeStamp) > _keepAliveInterval)
            {
                // Haven't sent a message for the entire keep-alive duration, so send a ping.
                // If the transport channel is full, this will fail, but that's OK because
                // adding a Ping message when the transport is full is unnecessary since the
                // transport is still in the process of sending frames.
                _ = TryWritePingAsync();

                // We only update the timestamp here, because updating on each sent message is bad for performance
                // There can be a lot of sent messages per 15 seconds
                Volatile.Write(ref _lastSendTimeStamp, currentTime);
            }
        }

        internal void StartClientTimeout()
        {
            if (_clientTimeoutActive)
            {
                return;
            }
            _clientTimeoutActive = true;
            Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).CheckClientTimeout(), this);
        }

        private void CheckClientTimeout()
        {
            if (Debugger.IsAttached)
            {
                return;
            }

            if (_useAbsoluteClientTimeout)
            {
                // If it's been too long since we've heard from the client, then close this
                if (DateTime.UtcNow.Ticks - Volatile.Read(ref _lastReceivedTimeStamp) > _clientTimeoutInterval)
                {
                    if (!_receivedMessageThisInterval)
                    {
                        Log.ClientTimeout(_logger, TimeSpan.FromTicks(_clientTimeoutInterval));
                        AbortAllowReconnect();
                    }

                    _receivedMessageThisInterval = false;
                    Volatile.Write(ref _lastReceivedTimeStamp, DateTime.UtcNow.Ticks);
                }
            }
            else
            {
                lock (_receiveMessageTimeoutLock)
                {
                    if (_receivedMessageTimeoutEnabled)
                    {
                        _receivedMessageElapsedTicks = DateTime.UtcNow.Ticks - _receivedMessageTimestamp;

                        if (_receivedMessageElapsedTicks >= _clientTimeoutInterval)
                        {
                            Log.ClientTimeout(_logger, TimeSpan.FromTicks(_clientTimeoutInterval));
                            AbortAllowReconnect();
                        }
                    }
                }
            }
        }

        private static void AbortConnection(object state)
        {
            var connection = (HubConnectionContext)state;

            try
            {
                connection._connectionAbortedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.AbortFailed(connection._logger, ex);
            }
            finally
            {
                _ = InnerAbortConnection(connection);

                // Use _streamTracker to avoid lazy init from StreamTracker getter if it doesn't exist
                if (connection._streamTracker != null)
                {
                    connection._streamTracker.CompleteAll(new OperationCanceledException("The underlying connection was closed."));
                }
            }

            static async Task InnerAbortConnection(HubConnectionContext connection)
            {
                // We lock to make sure all writes are done before triggering the completion of the pipe
                await connection._writeLock.WaitAsync();
                try
                {
                    // Communicate the fact that we're finished triggering abort callbacks
                    // HubOnDisconnectedAsync is waiting on this to complete the Pipe
                    connection._abortCompletedTcs.TrySetResult(null);
                }
                finally
                {
                    connection._writeLock.Release();
                }
            }
        }

        internal void ResetClientTimeout()
        {
            _receivedMessageThisInterval = true;
        }

        internal void BeginClientTimeout()
        {
            // check if new timeout behavior is in use
            if (!_useAbsoluteClientTimeout)
            {
                lock (_receiveMessageTimeoutLock)
                {
                    _receivedMessageTimeoutEnabled = true;
                    _receivedMessageTimestamp = DateTime.UtcNow.Ticks;
                }
            }
        }

        internal void StopClientTimeout()
        {
            // check if new timeout behavior is in use
            if (!_useAbsoluteClientTimeout)
            {
                lock (_receiveMessageTimeoutLock)
                {
                    // we received a message so stop the timer and reset it
                    // it will resume after the message has been processed
                    _receivedMessageElapsedTicks = 0;
                    _receivedMessageTimestamp = 0;
                    _receivedMessageTimeoutEnabled = false;
                }
            }
        }

        private static class Log
        {
            // Category: HubConnectionContext
            private static readonly Action<ILogger, string, Exception> _handshakeComplete =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "HandshakeComplete"), "Completed connection handshake. Using HubProtocol '{Protocol}'.");

            private static readonly Action<ILogger, Exception> _handshakeCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "HandshakeCanceled"), "Handshake was canceled.");

            private static readonly Action<ILogger, Exception> _sentPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "SentPing"), "Sent a ping message to the client.");

            private static readonly Action<ILogger, Exception> _transportBufferFull =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "TransportBufferFull"), "Unable to send Ping message to client, the transport buffer is full.");

            private static readonly Action<ILogger, Exception> _handshakeFailed =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "HandshakeFailed"), "Failed connection handshake.");

            private static readonly Action<ILogger, Exception> _failedWritingMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "FailedWritingMessage"), "Failed writing message. Aborting connection.");

            private static readonly Action<ILogger, string, int, Exception> _protocolVersionFailed =
                LoggerMessage.Define<string, int>(LogLevel.Debug, new EventId(7, "ProtocolVersionFailed"), "Server does not support version {Version} of the {Protocol} protocol.");

            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(8, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, int, Exception> _clientTimeout =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(9, "ClientTimeout"), "Client timeout ({ClientTimeout}ms) elapsed without receiving a message from the client. Closing connection.");

            private static readonly Action<ILogger, long, Exception> _handshakeSizeLimitExceeded =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(10, "HandshakeSizeLimitExceeded"), "The maximum message size of {MaxMessageSize}B was exceeded while parsing the Handshake. The message size can be configured in AddHubOptions.");

            public static void HandshakeComplete(ILogger logger, string hubProtocol)
            {
                _handshakeComplete(logger, hubProtocol, null);
            }

            public static void HandshakeCanceled(ILogger logger)
            {
                _handshakeCanceled(logger, null);
            }

            public static void SentPing(ILogger logger)
            {
                _sentPing(logger, null);
            }

            public static void TransportBufferFull(ILogger logger)
            {
                _transportBufferFull(logger, null);
            }

            public static void HandshakeFailed(ILogger logger, Exception exception)
            {
                _handshakeFailed(logger, exception);
            }

            public static void FailedWritingMessage(ILogger logger, Exception exception)
            {
                _failedWritingMessage(logger, exception);
            }

            public static void ProtocolVersionFailed(ILogger logger, string protocolName, int version)
            {
                _protocolVersionFailed(logger, protocolName, version, null);
            }

            public static void AbortFailed(ILogger logger, Exception exception)
            {
                _abortFailed(logger, exception);
            }

            public static void ClientTimeout(ILogger logger, TimeSpan timeout)
            {
                _clientTimeout(logger, (int)timeout.TotalMilliseconds, null);
            }

            public static void HandshakeSizeLimitExceeded(ILogger logger, long maxMessageSize)
            {
                _handshakeSizeLimitExceeded(logger, maxMessageSize, null);
            }
        }
    }
}
