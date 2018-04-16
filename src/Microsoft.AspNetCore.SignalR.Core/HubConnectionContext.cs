// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
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
    public class HubConnectionContext
    {
        private static readonly Action<object> _abortedCallback = AbortConnection;

        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly long _keepAliveDuration;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);

        private long _lastSendTimestamp = Stopwatch.GetTimestamp();
        private ReadOnlyMemory<byte> _cachedPingMessage;

        public HubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory)
        {
            _connectionContext = connectionContext;
            _logger = loggerFactory.CreateLogger<HubConnectionContext>();
            ConnectionAborted = _connectionAbortedTokenSource.Token;
            _keepAliveDuration = (int)keepAliveInterval.TotalMilliseconds * (Stopwatch.Frequency / 1000);
        }

        public virtual CancellationToken ConnectionAborted { get; }

        public virtual string ConnectionId => _connectionContext.ConnectionId;

        public virtual ClaimsPrincipal User => Features.Get<IConnectionUserFeature>()?.User;

        public virtual IFeatureCollection Features => _connectionContext.Features;

        public virtual IDictionary<object, object> Items => _connectionContext.Items;

        // Used by HubConnectionHandler
        internal PipeReader Input => _connectionContext.Transport.Input;

        public string UserIdentifier { get; set; }

        internal virtual IHubProtocol Protocol { get; set; }

        internal ExceptionDispatchInfo AbortException { get; private set; }

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.Ordinal);

        public virtual ValueTask WriteAsync(HubMessage message)
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message));
            }

            // This method should never throw synchronously
            var task = WriteCore(message);

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
        /// <returns></returns>
        public virtual ValueTask WriteAsync(SerializedHubMessage message)
        {
            // Try to grab the lock synchronously, if we fail, go to the slower path
            if (!_writeLock.Wait(0))
            {
                return new ValueTask(WriteSlowAsync(message));
            }

            // This method should never throw synchronously
            var task = WriteCore(message);

            // The write didn't complete synchronously so await completion
            if (!task.IsCompletedSuccessfully)
            {
                return new ValueTask(CompleteWriteAsync(task));
            }

            // Otherwise, release the lock acquired when entering WriteAsync
            _writeLock.Release();

            return default;
        }

        private ValueTask<FlushResult> WriteCore(HubMessage message)
        {
            try
            {
                // We know that we are only writing this message to one receiver, so we can
                // write it without caching.
                Protocol.WriteMessage(message, _connectionContext.Transport.Output);

                return _connectionContext.Transport.Output.FlushAsync();
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);

                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: true));
            }
        }

        private ValueTask<FlushResult> WriteCore(SerializedHubMessage message)
        {
            try
            {
                // Grab a preserialized buffer for this protocol.
                var buffer = message.GetSerializedMessage(Protocol);

                return _connectionContext.Transport.Output.WriteAsync(buffer);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);

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
            }
            finally
            {
                // Release the lock acquired when entering WriteAsync
                _writeLock.Release();
            }
        }

        private async Task WriteSlowAsync(HubMessage message)
        {
            await _writeLock.WaitAsync();
            try
            {
                // Failed to get the lock immediately when entering WriteAsync so await until it is available

                await WriteCore(message);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task WriteSlowAsync(SerializedHubMessage message)
        {
            try
            {
                // Failed to get the lock immediately when entering WriteAsync so await until it is available
                await _writeLock.WaitAsync();

                await WriteCore(message);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
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

            return new ValueTask(TryWritePingSlowAsync());
        }

        private async Task TryWritePingSlowAsync()
        {
            try
            {
                await _connectionContext.Transport.Output.WriteAsync(_cachedPingMessage);

                Log.SentPing(_logger);
            }
            catch (Exception ex)
            {
                Log.FailedWritingMessage(_logger, ex);
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
                if (message == HandshakeResponseMessage.Empty)
                {
                    // success response is always an empty object so send cached data
                    _connectionContext.Transport.Output.Write(HandshakeProtocol.SuccessHandshakeData.Span);
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

        public virtual void Abort()
        {
            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            // We fire and forget since this can trigger user code to run
            Task.Factory.StartNew(_abortedCallback, this);
        }

        internal async Task<bool> HandshakeAsync(TimeSpan timeout, IReadOnlyList<string> supportedProtocols, IHubProtocolResolver protocolResolver,
            IUserIdProvider userIdProvider, bool enableDetailedErrors)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    if (!Debugger.IsAttached)
                    {
                        cts.CancelAfter(timeout);
                    }

                    while (true)
                    {
                        var result = await _connectionContext.Transport.Input.ReadAsync(cts.Token);
                        var buffer = result.Buffer;
                        var consumed = buffer.Start;
                        var examined = buffer.End;

                        try
                        {
                            if (!buffer.IsEmpty)
                            {
                                if (HandshakeProtocol.TryParseRequestMessage(ref buffer, out var handshakeRequestMessage))
                                {
                                    // We parsed the handshake
                                    consumed = buffer.Start;
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
                                else
                                {
                                    _logger.LogInformation("Didn't parse the handshake");
                                }
                            }
                            else if (result.IsCompleted)
                            {
                                // connection was closed before we ever received a response
                                // can't send a handshake response because there is no longer a connection
                                Log.HandshakeFailed(_logger, null);
                                return false;
                            }
                        }
                        finally
                        {
                            _connectionContext.Transport.Input.AdvanceTo(consumed, examined);
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

        internal void Abort(Exception exception)
        {
            AbortException = ExceptionDispatchInfo.Capture(exception);
            Abort();
        }

        // Used by the HubConnectionHandler only
        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }

        private void KeepAliveTick()
        {
            var timestamp = Stopwatch.GetTimestamp();
            // Implements the keep-alive tick behavior
            // Each tick, we check if the time since the last send is larger than the keep alive duration (in ticks).
            // If it is, we send a ping frame, if not, we no-op on this tick. This means that in the worst case, the
            // true "ping rate" of the server could be (_hubOptions.KeepAliveInterval + HubEndPoint.KeepAliveTimerInterval),
            // because if the interval elapses right after the last tick of this timer, it won't be detected until the next tick.
            if (timestamp - Interlocked.Read(ref _lastSendTimestamp) > _keepAliveDuration)
            {
                // Haven't sent a message for the entire keep-alive duration, so send a ping.
                // If the transport channel is full, this will fail, but that's OK because
                // adding a Ping message when the transport is full is unnecessary since the
                // transport is still in the process of sending frames.
                _ = TryWritePingAsync();

                Interlocked.Exchange(ref _lastSendTimestamp, timestamp);
            }
        }

        private static void AbortConnection(object state)
        {
            var connection = (HubConnectionContext)state;
            try
            {
                connection._connectionAbortedTokenSource.Cancel();

                // Communicate the fact that we're finished triggering abort callbacks
                connection._abortCompletedTcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                // TODO: Should we log if the cancellation callback fails? This is more preventative to make sure
                // we don't end up with an unobserved task
                connection._abortCompletedTcs.TrySetException(ex);
            }
        }

        private static class Log
        {
            // Category: HubConnectionContext
            private static readonly Action<ILogger, string, Exception> _handshakeComplete =
                LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "HandshakeComplete"), "Completed connection handshake. Using HubProtocol '{Protocol}'.");

            private static readonly Action<ILogger, Exception> _handshakeCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "HandshakeCanceled"), "Handshake was canceled.");

            private static readonly Action<ILogger, Exception> _sentPing =
                LoggerMessage.Define(LogLevel.Trace, new EventId(3, "SentPing"), "Sent a ping message to the client.");

            private static readonly Action<ILogger, Exception> _transportBufferFull =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "TransportBufferFull"), "Unable to send Ping message to client, the transport buffer is full.");

            private static readonly Action<ILogger, Exception> _handshakeFailed =
                LoggerMessage.Define(LogLevel.Error, new EventId(5, "HandshakeFailed"), "Failed connection handshake.");

            private static readonly Action<ILogger, Exception> _failedWritingMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(6, "FailedWritingMessage"), "Failed writing message.");

            private static readonly Action<ILogger, string, int, Exception> _protocolVersionFailed =
                LoggerMessage.Define<string, int>(LogLevel.Warning, new EventId(7, "ProtocolVersionFailed"), "Server does not support version {Version} of the {Protocol} protocol.");

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
        }

    }
}
