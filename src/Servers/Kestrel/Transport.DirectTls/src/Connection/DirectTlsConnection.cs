// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

/// <summary>
/// Connection context for an established DirectTls connection.
///
/// After the handshake completes:
/// - Owns the <see cref="ConnectionIoState"/> (which drives the connection's <see cref="System.Net.Security.TlsSocketSession"/>)
/// - Uses the assigned <see cref="TlsEventPump"/> for all I/O (read/write via epoll)
/// - Backend operations happen on the pump's dedicated thread
/// - Completions are dispatched to the ThreadPool where pipelines run
/// </summary>
internal sealed partial class DirectTlsConnection : TransportConnection
{
    // Mirrors the sockets transport's SocketConnection.MinAllocBufferSize (PinnedBlockMemoryPool.BlockSize / 2).
    // Avoids defragmentation of the transport's shared memory pool
    private const int MinAllocBufferSize = 4096 / 2;

    private readonly ConnectionIoState _connectionState;
    private readonly TlsEventPump _pump;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new();

    private Task? _receiveTask;
    private Task? _sendTask;
    private volatile bool _aborted;
    private int _disposed; // 0 = not disposed, 1 = disposed (for thread-safe Compare-And-Swap)

    // The accepted client leaf certificate whose lifetime this connection owns, so it can be disposed
    // exactly once on teardown. On the accept path the runtime's TlsSession transfers ownership of the leaf
    // to the caller: SetRemoteCertificateValidationResult(None) promotes it into TlsSession._remoteCertificate,
    // which TlsSession.Dispose() deliberately never frees (unlike _externalPendingCert and the intermediates).
    // Tracked separately from the settable ITlsConnectionFeature.ClientCertificate property so that we always
    // dispose the cert the runtime handed us - and never an object an app may have reassigned onto the feature.
    private X509Certificate2? _ownedClientCertificate;

    public DirectTlsConnection(
        ConnectionIoState connectionState,
        TlsEventPump pump,
        EndPoint? localEndPoint,
        EndPoint? remoteEndPoint,
        MemoryPool<byte> memoryPool,
        long maxReadBufferSize,
        long maxWriteBufferSize,
        ILogger logger,
        SslApplicationProtocol negotiatedApplicationProtocol = default,
        X509Certificate2? clientCertificate = null)
    {
        _connectionState = connectionState;
        _pump = pump;
        _memoryPool = memoryPool;
        _logger = logger;

        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
        ConnectionClosed = _connectionClosedTokenSource.Token;

        // A managed Socket over the raw fd (non-owning) is offered, matching the standard sockets
        // transport's IConnectionSocketFeature.
        Features.Set<IConnectionSocketFeature>(this);

        // Mark the connection as TLS-secured. The TLS feature interfaces are implemented directly on this
        // connection (see DirectTlsConnection.FeatureCollection.cs), mirroring how SocketConnection exposes
        // IConnectionSocketFeature and how the SslStream path's TlsConnectionFeature backs every TLS feature
        // off a single object. Their presence makes the UseHttps middleware no-op (it sees an existing
        // ITlsConnectionFeature instead of wrapping the transport in a second SslStream) and makes Kestrel
        // resolve the request scheme as https. The handshake is already complete here. When the endpoint
        // requested a client certificate (mTLS) and the peer presented one that passed validation, it is
        // stored in ClientCertificate so HttpContext.Connection.ClientCertificate resolves.
        ClientCertificate = clientCertificate;
        _ownedClientCertificate = clientCertificate;
        Features.Set<ITlsConnectionFeature>(this);
        Features.Set<ITlsHandshakeFeature>(this);

        // Publish the ALPN protocol negotiated during the handshake. This transport references Kestrel.Core,
        // so ITlsApplicationProtocolFeature is set directly (read live from _negotiatedApplicationProtocol),
        // and HttpConnection.SelectProtocol can pick HTTP/2 without the UseHttps middleware on the path. The
        // value is refreshed at CompleteHandshake for connections allocated early (at NeedsTlsContext).
        _negotiatedApplicationProtocol = negotiatedApplicationProtocol;
        Features.Set<ITlsApplicationProtocolFeature>(this);

        // Subscribe to fatal errors from the connection I/O state
        // This ensures we get notified even if no read/write is pending when peer disconnects
        _connectionState.OnFatalError = OnTlsFatalError;

        // Create duplex pipe pair for Kestrel. MaxReadBufferSize / MaxWriteBufferSize become the writer
        // backpressure thresholds so a slow app (input pipe) or a slow/blocked peer (output pipe) can't force
        // unbounded server buffering - matching the sockets transport.
        var (inputOptions, outputOptions) = CreatePipeOptions(memoryPool, maxReadBufferSize, maxWriteBufferSize);

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);
        Transport = pair.Transport;
        Application = pair.Application;
    }

    /// <summary>
    /// Builds the input/output <see cref="PipeOptions"/> for the connection's duplex pipe pair.
    /// <paramref name="maxReadBufferSize"/> and <paramref name="maxWriteBufferSize"/> are applied as writer
    /// backpressure thresholds - the writer pauses at the maximum and resumes once drained to half of it. A
    /// size of 0 disables backpressure for that direction (unbounded buffering), matching the sockets
    /// transport's <c>MaxReadBufferSize</c> / <c>MaxWriteBufferSize</c> semantics.
    /// </summary>
    /// <remarks>
    /// The scheduler split matches the inline-transport sockets path: the input pipe (decrypted reads the app
    /// consumes) dispatches reader continuations to the thread pool and writes inline on the pump thread; the
    /// output pipe (app writes the pump encrypts) does the reverse.
    /// </remarks>
    internal static (PipeOptions InputOptions, PipeOptions OutputOptions) CreatePipeOptions(
        MemoryPool<byte> memoryPool,
        long maxReadBufferSize,
        long maxWriteBufferSize)
    {
        var inputOptions = new PipeOptions(
            pool: memoryPool,
            readerScheduler: PipeScheduler.ThreadPool,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: maxReadBufferSize,
            resumeWriterThreshold: maxReadBufferSize / 2,
            useSynchronizationContext: false);

        var outputOptions = new PipeOptions(
            pool: memoryPool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.ThreadPool,
            pauseWriterThreshold: maxWriteBufferSize,
            resumeWriterThreshold: maxWriteBufferSize / 2,
            useSynchronizationContext: false);

        return (inputOptions, outputOptions);
    }

    public override MemoryPool<byte> MemoryPool => _memoryPool;

    /// <summary>
    /// The underlying per-connection TLS state. Exposed so the pump can promote a connection that was
    /// allocated early (at NeedsTlsContext, for the ClientHello listener) to an established connection.
    /// </summary>
    internal ConnectionIoState ConnectionState => _connectionState;

    /// <summary>
    /// Promotes a connection that was allocated early (at NeedsTlsContext, so the ClientHello listener
    /// had a stable <see cref="ConnectionContext"/>) to a fully-established connection by publishing the
    /// values that were unknown at allocation time: the negotiated ALPN protocol and any validated client
    /// certificate. The TLS feature interfaces were already wired in the constructor and read the
    /// remaining negotiated values live from the session, so nothing else needs updating.
    /// </summary>
    internal void CompleteHandshake(SslApplicationProtocol negotiatedApplicationProtocol, X509Certificate2? clientCertificate)
    {
        ClientCertificate = clientCertificate;
        _ownedClientCertificate = clientCertificate;
        _negotiatedApplicationProtocol = negotiatedApplicationProtocol;
    }

    /// <summary>
    /// Tears down a connection that was allocated early (at NeedsTlsContext) but whose handshake never
    /// completed - for example the ClientHello listener fired and then a later handshake step failed.
    /// The receive/send loops were never <see cref="Start"/>ed, so this just completes the (idle) pipes
    /// and closes the socket fd directly. It deliberately does NOT go through
    /// <see cref="ConnectionIoState.Dispose"/>, which would attempt a graceful close_notify
    /// (<c>Shutdown()</c>) that a half-open session cannot perform cleanly.
    /// </summary>
    internal void AbortBeforeStart()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }
        _aborted = true;

        Application.Input.Complete();
        Application.Output.Complete();
        Transport.Input.Complete();
        Transport.Output.Complete();

        try
        {
            // Close the socket fd of the half-open handshake without the graceful close_notify.
            lock (_socketLock)
            {
                _connectionState.Session.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to dispose half-open connection backend for fd={Fd}", _connectionState.Fd);
        }

        CancelConnectionClosedToken();

        // A half-open handshake never reached mTLS validation, so _ownedClientCertificate is normally null
        // here; dispose defensively (no-op when null) to keep both teardown paths symmetric.
        _ownedClientCertificate?.Dispose();

        // Dispose the cached IConnectionSocketFeature wrapper if one was materialized (non-owning, so this
        // never closes the fd) to keep both teardown paths symmetric.
        _socket?.Dispose();
    }

    /// <summary>
    /// Start the receive and send loops.
    /// </summary>
    public void Start()
    {
        _receiveTask = ReceiveLoopAsync();
        _sendTask = SendLoopAsync();
    }

    /// <summary>
    /// Receive loop: SSL_read -> write to Application.Output (Kestrel reads from Transport.Input)
    /// Uses the pump's epoll-based async SSL_read.
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_aborted)
            {
                var memory = Application.Output.GetMemory(MinAllocBufferSize);

                // Use pump's async SSL_read (waits for epoll event, does SSL_read on pump thread)
                int bytesRead = await _connectionState.ReadAsync(memory);

                if (bytesRead > 0)
                {
                    Application.Output.Advance(bytesRead);

                    var flushTask = Application.Output.FlushAsync();
                    FlushResult flushResult;
                    if (flushTask.IsCompleted)
                    {
                        flushResult = await flushTask;
                    }
                    else
                    {
                        // Backpressure: the application isn't draining the input pipe, so no read is pending. Suspend
                        // readable interest while we wait - otherwise still-buffered ciphertext keeps the level-triggered
                        // pump returning EPOLLIN every loop and spins the worker. Re-arm once the flush unblocks.
                        _connectionState.SuspendReadInterest();
                        try
                        {
                            flushResult = await flushTask;
                        }
                        finally
                        {
                            _connectionState.ResumeReadInterest();
                        }
                    }

                    if (flushResult.IsCompleted || flushResult.IsCanceled)
                    {
                        break;
                    }
                }
                else if (bytesRead == 0)
                {
                    // Connection closed (EOF)
                    break;
                }
                else
                {
                    // Negative = error (shouldn't happen with async API, but handle it)
                    error = new IOException($"SSL_read failed with {bytesRead}");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            Application.Output.Complete(error);
        }
    }

    /// <summary>
    /// Send loop: read from Application.Input (Kestrel writes to Transport.Output) -> SSL_write
    /// Uses the pump's epoll-based async SSL_write.
    /// </summary>
    private async Task SendLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_aborted)
            {
                var result = await Application.Input.ReadAsync();

                // Check for cancellation first
                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;

                // Process buffer data BEFORE checking IsCompleted
                // This ensures the final chunk (e.g., "0\\r\\n\\r\\n" terminator) is sent
                if (!buffer.IsEmpty)
                {
                    foreach (var segment in buffer)
                    {
                        if (segment.Length > 0)
                        {
                            // Use pump's async SSL_write (waits for epoll event if needed)
                            // Pump handles the SSL_write on its dedicated thread
                            var written = await _connectionState.WriteAsync(segment);
                            if (written == 0)
                            {
                                // Peer closed the connection mid-send.
                                Application.Input.AdvanceTo(buffer.End);
                                return;
                            }
                        }
                    }
                }

                Application.Input.AdvanceTo(buffer.End);

                // Check completion AFTER processing and advancing (matches Kestrel's DoSend pattern)
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            Application.Input.Complete(error);
        }
    }

    public override void Abort(ConnectionAbortedException abortReason)
    {
        if (_aborted)
        {
            return;
        }
        _aborted = true;

        // Unblock BOTH loops so the connection tears down immediately instead of leaving the socket
        // half-open until the peer times out. SocketConnection.Abort closes the socket synchronously
        // (RST/FIN); DirectTls instead closes it later in DisposeAsync, so Abort must make Kestrel
        // progress to that point right away.
        //
        // The receive loop is the key: on an app-initiated abort mid-response (e.g. HttpContext.Abort),
        // it is parked in ReadAsync waiting for peer bytes while the peer is parked waiting for response
        // bytes - a standoff only the peer's request timeout can break. Cancelling the TLS operations
        // completes that parked read; the receive loop then completes Application.Output, signalling
        // Kestrel that the transport is done so it disposes the connection (closing the socket) without
        // waiting. Cancelling the pending flush covers a receive loop parked on Application.Output
        // backpressure, and CancelPendingRead unblocks the send loop as before.
        _connectionState.Cancel();
        Application.Input.CancelPendingRead();
        Application.Output.CancelPendingFlush();
    }

    /// <summary>
    /// Called when the TLS connection encounters a fatal error (e.g., peer disconnect via EPOLLRDHUP).
    /// This just aborts the connection - disposal will happen when Kestrel calls DisposeAsync.
    /// </summary>
    private void OnTlsFatalError(Exception ex)
    {
        if (_aborted || Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        _logger.LogDebug(ex, "TLS fatal error for fd={Fd}, aborting connection", _connectionState.Fd);

        // Just abort to cancel pending operations - don't trigger disposal here. Kestrel calls DisposeAsync
        // when it's done with the connection, which prevents premature disposal while SendLoop is still writing.
        // Abort only cancels awaitables and pipe reads/flushes (none of which throw), so no guard is needed here.
        Abort(new ConnectionAbortedException("TLS connection error", ex));
    }

    private void CancelConnectionClosedToken()
    {
        try
        {
            _connectionClosedTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            // a throwing callback must not escape and abandon the rest of the teardown
            _logger.LogError(0, ex, $"Unexpected exception in {nameof(DirectTlsConnection)}.{nameof(CancelConnectionClosedToken)}.");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // Thread-safe check: only one call to DisposeAsync proceeds
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        // 1. Cancel pending TLS operations (unblocks ReadAsync/WriteAsync TCS)
        _connectionState.Cancel();

        // 2. Unregister from pump (removes from epoll, prevents new events)
        _pump.Unregister(_connectionState.Fd);

        // 3. Cancel pending pipeline operations to unblock our loops
        Application.Input.CancelPendingRead();
        Application.Output.CancelPendingFlush();

        // 4. Wait for loops to finish (they should complete quickly now)
        if (_receiveTask != null)
        {
            await _receiveTask.ConfigureAwait(false);
        }
        if (_sendTask != null)
        {
            await _sendTask.ConfigureAwait(false);
        }

        // 5. Complete the transport pipes (signals to Kestrel)
        Transport.Input.Complete();
        Transport.Output.Complete();

        // 6. Graceful TLS and socket shutdown. Disposing the connection state sends
        //    close_notify via the TLS session and closes the underlying socket fd
        //    (the session owns the SafeSocketHandle), so no manual shutdown/close here.
        try
        {
            lock (_socketLock)
            {
                _connectionState.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "TLS shutdown failed for fd={Fd}", _connectionState.Fd);
        }

        // 7. Signal connection closed
        CancelConnectionClosedToken();
        _connectionClosedTokenSource.Dispose();

        // 8. Dispose the accepted client certificate. The runtime's TlsSession transferred ownership of the
        //    leaf to us on the accept path (see _ownedClientCertificate) and never frees it itself, so the
        //    transport must, or the native key handle leaks once per accepted mTLS connection. Kestrel is done
        //    with the connection by now, mirroring how SslStream disposes RemoteCertificate with the stream.
        _ownedClientCertificate?.Dispose();

        // 9. Dispose the cached IConnectionSocketFeature wrapper, if one was ever materialized. It is non-owning
        //    so this never closes the fd (the session already did, above), but it marks the wrapper disposed so
        //    any late metadata read fails loudly instead of operating on a descriptor the OS may have recycled.
        _socket?.Dispose();
    }
}
