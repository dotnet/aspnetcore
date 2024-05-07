// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

internal partial class QuicConnectionContext : TransportMultiplexedConnection
{
    // Internal for testing.
    internal PooledStreamStack<QuicStreamContext> StreamPool;

    private bool _streamPoolHeartbeatInitialized;
    // Ticks updated once per-second in heartbeat event.
    private long _heartbeatTimestamp;
    private readonly object _poolLock = new object();

    private readonly object _shutdownLock = new object();
    private readonly QuicConnection _connection;
    private readonly QuicTransportContext _context;
    private readonly ILogger _log;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

    private Task? _closeTask;
    private ExceptionDispatchInfo? _abortReason;

    internal const int InitialStreamPoolSize = 5;
    internal const int MaxStreamPoolSize = 100;
    internal const long StreamPoolExpirySeconds = 5;

    public QuicConnectionContext(QuicConnection connection, QuicTransportContext context)
    {
        _log = context.Log;
        _context = context;
        _connection = connection;
        ConnectionClosed = _connectionClosedTokenSource.Token;

        StreamPool = new PooledStreamStack<QuicStreamContext>(InitialStreamPoolSize);

        RemoteEndPoint = connection.RemoteEndPoint;
        LocalEndPoint = connection.LocalEndPoint;

        InitializeFeatures();
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            lock (_shutdownLock)
            {
                // The DefaultCloseErrorCode setter validates that the error code is within the valid range
                _closeTask ??= _connection.CloseAsync(errorCode: _context.Options.DefaultCloseErrorCode).AsTask();
            }

            await _closeTask;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to gracefully shutdown connection.");
        }

        await _connection.DisposeAsync();
    }

    public override void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via MultiplexedConnectionContext.Abort()."));

    public override void Abort(ConnectionAbortedException abortReason)
    {
        lock (_shutdownLock)
        {
            // Check if connection has already been already aborted.
            if (_abortReason != null || _closeTask != null)
            {
                return;
            }

            var resolvedErrorCode = _error ?? 0; // Only valid error codes are assigned to _error
            _abortReason = ExceptionDispatchInfo.Capture(abortReason);
            QuicLog.ConnectionAbort(_log, this, resolvedErrorCode, abortReason.Message);
            _closeTask = _connection.CloseAsync(errorCode: resolvedErrorCode).AsTask();
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public override async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = await _connection.AcceptInboundStreamAsync(cancellationToken);

            QuicStreamContext? context = null;

            // Only use pool for bidirectional streams. Just a handful of unidirecitonal
            // streams are created for a connection and they live for the lifetime of the connection.
            if (stream.CanRead && stream.CanWrite)
            {
                lock (_poolLock)
                {
                    StreamPool.TryPop(out context);
                }
            }

            if (context == null)
            {
                context = new QuicStreamContext(this, _context);
                context.Initialize(stream);
            }
            else
            {
                context.ResetFeatureCollection();
                context.ResetItems();
                context.Initialize(stream);

                QuicLog.StreamReused(_log, context);
            }

            context.Start();

            QuicLog.AcceptedStream(_log, context);

            return context;
        }
        catch (QuicException ex) when (ex.QuicError == QuicError.ConnectionAborted)
        {
            // Shutdown initiated by peer, abortive.
            _error = ex.ApplicationErrorCode; // Trust Quic to provide us a valid error code
            QuicLog.ConnectionAborted(_log, this, ex.ApplicationErrorCode.GetValueOrDefault(), ex);

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                state.CancelConnectionClosedToken();
            },
            this,
            preferLocal: false);

            // Throw error so consumer sees the connection is aborted by peer.
            throw new ConnectionResetException(ex.Message, ex);
        }
        catch (QuicException ex) when (ex.QuicError == QuicError.OperationAborted)
        {
            lock (_shutdownLock)
            {
                // OperationAborted should only happen when shutdown has been initiated by the server.
                // If there is no abort reason and we have this error then the connection is in an
                // unexpected state. Abort connection and throw reason error.
                if (_abortReason == null)
                {
                    Abort(new ConnectionAbortedException("Unexpected error when accepting stream.", ex));
                }

                _abortReason!.Throw();
            }
        }
        catch (QuicException ex) when (ex.QuicError == QuicError.ConnectionTimeout)
        {
            lock (_shutdownLock)
            {
                // ConnectionTimeout can happen when the client app is shutdown without aborting the connection.
                // For example, a console app makes a HTTP/3 request with HttpClient and then exits without disposing the client.
                if (_abortReason == null)
                {
                    Abort(new ConnectionAbortedException("The connection timed out waiting for a response from the peer.", ex));
                }

                _abortReason!.Throw();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Assert(cancellationToken.IsCancellationRequested, "Error requires cancellation is requested.");

            lock (_shutdownLock)
            {
                // Connection has been aborted. Throw reason exception.
                _abortReason?.Throw();
            }
        }
#if DEBUG
        catch (Exception ex)
        {
            Debug.Fail($"Unexpected exception in {nameof(QuicConnectionContext)}.{nameof(AcceptAsync)}: {ex}");
            throw;
        }
#endif

        // Return null for graceful closure or cancellation.
        return null;
    }

    private void CancelConnectionClosedToken()
    {
        try
        {
            _connectionClosedTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            _log.LogError(0, ex, $"Unexpected exception in {nameof(QuicConnectionContext)}.{nameof(CancelConnectionClosedToken)}.");
        }
    }

    public override async ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default)
    {
        QuicStream quicStream;

        var streamDirectionFeature = features?.Get<IStreamDirectionFeature>();
        if (streamDirectionFeature != null)
        {
            if (streamDirectionFeature.CanRead)
            {
                quicStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken);
            }
            else
            {
                quicStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional, cancellationToken);
            }
        }
        else
        {
            quicStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken);
        }

        // Only a handful of control streams are created by the server and they last for the
        // lifetime of the connection. No value in pooling them.
        QuicStreamContext? context = new QuicStreamContext(this, _context);
        context.Initialize(quicStream);
        context.Start();

        QuicLog.ConnectedStream(_log, context);

        return context;
    }

    internal bool TryReturnStream(QuicStreamContext stream)
    {
        lock (_poolLock)
        {
            var timeProvider = _context.Options.TimeProvider;

            if (!_streamPoolHeartbeatInitialized)
            {
                // Heartbeat feature is added to connection features by Kestrel.
                // No event is on the context is raised between feature being added and serving
                // connections so initialize heartbeat the first time a stream is added to
                // the connection's stream pool.
                var heartbeatFeature = Features.Get<IConnectionHeartbeatFeature>();
                if (heartbeatFeature == null)
                {
                    throw new InvalidOperationException($"Required {nameof(IConnectionHeartbeatFeature)} not found in connection features.");
                }

                heartbeatFeature.OnHeartbeat(static state => ((QuicConnectionContext)state).RemoveExpiredStreams(), this);

                // Set timestamp for the first time. Timestamps are then updated in heartbeat.
                var now = timeProvider.GetTimestamp();
                Volatile.Write(ref _heartbeatTimestamp, now);

                _streamPoolHeartbeatInitialized = true;
            }

            if (stream.CanReuse && StreamPool.Count < MaxStreamPoolSize)
            {
                stream.PoolExpirationTimestamp = Volatile.Read(ref _heartbeatTimestamp) + StreamPoolExpirySeconds * timeProvider.TimestampFrequency;
                StreamPool.Push(stream);

                QuicLog.StreamPooled(_log, stream);
                return true;
            }
        }

        return false;
    }

    internal QuicConnection GetInnerConnection()
    {
        return _connection;
    }

    private void RemoveExpiredStreams()
    {
        lock (_poolLock)
        {
            // Update ticks on heartbeat. A precise value isn't necessary.
            var now = _context.Options.TimeProvider.GetTimestamp();
            Volatile.Write(ref _heartbeatTimestamp, now);

            StreamPool.RemoveExpired(now);
        }
    }
}
