// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR.Core;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionContext
    {
        private static Action<object> _abortedCallback = AbortConnection;

        private readonly ConnectionContext _connectionContext;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>();
        private readonly long _keepAliveDuration;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);

        private long _lastSendTimestamp = Stopwatch.GetTimestamp();
        private byte[] _cachedPingMessage;

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

        public virtual PipeReader Input => _connectionContext.Transport.Input;

        public string UserIdentifier { get; private set; }

        internal virtual IHubProtocol Protocol { get; set; }

        internal ExceptionDispatchInfo AbortException { get; private set; }

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>();

        public virtual ValueTask WriteAsync(HubMessage message)
        {
            // We were unable to get the lock so take the slow async path of waiting for the semaphore
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
                // This will internally cache the buffer for each unique HubProtocol
                // So that we don't serialize the HubMessage for every single connection
                var buffer = message.WriteMessage(Protocol);

                _connectionContext.Transport.Output.Write(buffer);

                return _connectionContext.Transport.Output.FlushAsync();
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
                Debug.Assert(_cachedPingMessage != null);

                _connectionContext.Transport.Output.Write(_cachedPingMessage);

                await _connectionContext.Transport.Output.FlushAsync();

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
                var ms = new MemoryStream();
                HandshakeProtocol.WriteResponseMessage(message, ms);

                await _connectionContext.Transport.Output.WriteAsync(ms.ToArray());
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

        internal async Task<bool> HandshakeAsync(TimeSpan timeout, IList<string> supportedProtocols, IHubProtocolResolver protocolResolver, IUserIdProvider userIdProvider)
        {
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);

                    while (true)
                    {
                        var result = await _connectionContext.Transport.Input.ReadAsync(cts.Token);
                        var buffer = result.Buffer;
                        var consumed = buffer.End;
                        var examined = buffer.End;

                        try
                        {
                            if (!buffer.IsEmpty)
                            {
                                if (HandshakeProtocol.TryParseRequestMessage(buffer, out var handshakeRequestMessage, out consumed, out examined))
                                {
                                    Protocol = protocolResolver.GetProtocol(handshakeRequestMessage.Protocol, supportedProtocols, this);
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

                                    _cachedPingMessage = Protocol.WriteToArray(PingMessage.Instance);

                                    UserIdentifier = userIdProvider.GetUserId(this);

                                    if (Features.Get<IConnectionInherentKeepAliveFeature>() == null)
                                    {
                                        // Only register KeepAlive after protocol handshake otherwise KeepAliveTick could try to write without having a ProtocolReaderWriter
                                        Features.Get<IConnectionHeartbeatFeature>()?.OnHeartbeat(state => ((HubConnectionContext)state).KeepAliveTick(), this);
                                    }

                                    Log.HandshakeComplete(_logger, Protocol.Name);
                                    await WriteHandshakeResponseAsync(HandshakeResponseMessage.Empty);
                                    return true;
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
                await WriteHandshakeResponseAsync(new HandshakeResponseMessage($"An unexpected error occurred during connection handshake. {ex.GetType().Name}: {ex.Message}"));
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
