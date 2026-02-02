// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Uncomment the following line to enable debug counters for SSL diagnostics
// #define DIRECTSSL_DEBUG_COUNTERS

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Connection context that uses native OpenSSL for TLS.
///
/// After handshake completes:
/// - Owns the SslConnectionState (which contains SSL pointer)
/// - Uses the assigned SslEventPump for all I/O (read/write via epoll)
/// - SSL operations happen on the pump's dedicated thread
/// - Completions are dispatched to ThreadPool where pipelines run
/// </summary>
internal sealed class DirectSslConnection : TransportConnection
{
    private readonly int _fd;  // Raw fd - no Socket wrapper to avoid syscall overhead
    private readonly SslConnectionState _connectionState;
    private readonly SslEventPump _pump;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new();

    private Task? _receiveTask;
    private Task? _sendTask;
    private volatile bool _aborted;
    private int _disposed; // 0 = not disposed, 1 = disposed (for thread-safe Compare-And-Swap)

    public DirectSslConnection(
        int fd,
        SslConnectionState connectionState,
        SslEventPump pump,
        EndPoint? localEndPoint,
        EndPoint? remoteEndPoint,
        MemoryPool<byte> memoryPool,
        ILogger logger)
    {
        _fd = fd;
        _connectionState = connectionState;
        _pump = pump;
        _memoryPool = memoryPool;
        _logger = logger;

        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
        ConnectionClosed = _connectionClosedTokenSource.Token;

        // Subscribe to fatal errors from the SSL connection state
        // This ensures we get notified even if no read/write is pending when peer disconnects
        _connectionState.OnFatalError = OnSslFatalError;

        // Create duplex pipe pair for Kestrel
        var inputOptions = new PipeOptions(
            pool: memoryPool,
            readerScheduler: PipeScheduler.ThreadPool,
            writerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false);

        var outputOptions = new PipeOptions(
            pool: memoryPool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.ThreadPool,
            useSynchronizationContext: false);

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);
        Transport = pair.Transport;
        Application = pair.Application;
    }

    public override MemoryPool<byte> MemoryPool => _memoryPool;

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
                var memory = Application.Output.GetMemory();

                // Use pump's async SSL_read (waits for epoll event, does SSL_read on pump thread)
                int bytesRead = await _connectionState.ReadAsync(memory);

                if (bytesRead > 0)
                {
                    Application.Output.Advance(bytesRead);
                    var flushResult = await Application.Output.FlushAsync();
                    if (flushResult.IsCompleted || flushResult.IsCanceled)
                    {
                        break;
                    }
                }
                else if (bytesRead == 0)
                {
                    // Connection closed (EOF)
#if DIRECTSSL_DEBUG_COUNTERS
                    Interlocked.Increment(ref SslEventPump.TotalReadEof);
#endif
                    break;
                }
                else
                {
                    // Negative = error (shouldn't happen with async API, but handle it)
#if DIRECTSSL_DEBUG_COUNTERS
                    Interlocked.Increment(ref SslEventPump.TotalReadErrors);
#endif
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
#if DIRECTSSL_DEBUG_COUNTERS
            Interlocked.Increment(ref SslEventPump.TotalReadErrors);
#endif
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
                                // Peer closed connection
#if DIRECTSSL_DEBUG_COUNTERS
                                Interlocked.Increment(ref SslEventPump.TotalWriteEof);
#endif
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
#if DIRECTSSL_DEBUG_COUNTERS
            Interlocked.Increment(ref SslEventPump.TotalWriteErrors);
#endif
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

        // CTS may already be disposed if DisposeAsync completed first
        try
        {
            _connectionClosedTokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed, ignore
        }

        // Only cancel Application.Input to unblock SendLoop (matches Kestrel's Abort pattern)
        // Don't cancel Application.Output - let ReceiveLoop exit naturally
        Application.Input.CancelPendingRead();
    }

    /// <summary>
    /// Called when the SSL connection encounters a fatal error (e.g., peer disconnect via EPOLLRDHUP).
    /// This just aborts the connection - disposal will happen when Kestrel calls DisposeAsync.
    /// </summary>
    private void OnSslFatalError(Exception ex)
    {
        // Check if already disposed or aborted - connection may have been cleaned up already
        if (_aborted || Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            _logger.LogDebug(ex, "SSL fatal error for fd={Fd}, aborting connection", _connectionState.Fd);

            // Just abort to cancel pending operations - don't trigger disposal here
            // Kestrel will call DisposeAsync when it's done with the connection
            // This prevents premature disposal while SendLoop is still writing
            Abort(new ConnectionAbortedException("SSL connection error", ex));
        }
        catch (ObjectDisposedException)
        {
            // Race with DisposeAsync - connection is already being torn down
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // Thread-safe check: only one call to DisposeAsync proceeds
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        // 1. Cancel pending SSL operations (unblocks ReadAsync/WriteAsync TCS)
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

        // 6. Graceful SSL and socket shutdown (matching Kestrel's SocketConnection pattern)
        try
        {
            // SSL shutdown sends close_notify alert
            _connectionState.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SSL shutdown failed for fd={Fd}", _connectionState.Fd);
        }

        // Shutdown both directions using P/Invoke (avoids Socket wrapper overhead)
        // Ignore return value - socket may already be closed by peer
        NativeSsl.shutdown(_fd, NativeSsl.SHUT_RDWR);

        // Close the fd using P/Invoke
        NativeSsl.close(_fd);

        // 7. Signal connection closed
        _connectionClosedTokenSource.Cancel();
        _connectionClosedTokenSource.Dispose();
    }
}
