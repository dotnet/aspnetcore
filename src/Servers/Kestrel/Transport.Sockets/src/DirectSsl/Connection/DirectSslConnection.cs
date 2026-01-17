// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
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
    private readonly Socket _socket;
    private readonly SslConnectionState _connectionState;
    private readonly SslEventPump _pump;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new();

    private Task? _receiveTask;
    private Task? _sendTask;
    private volatile bool _aborted;
    private int _disposed; // 0 = not disposed, 1 = disposed (for thread-safe CAS)

    public DirectSslConnection(
        Socket socket,
        SslConnectionState connectionState,
        SslEventPump pump,
        EndPoint? localEndPoint,
        EndPoint? remoteEndPoint,
        MemoryPool<byte> memoryPool,
        ILogger logger)
    {
        _socket = socket;
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
                    _logger.LogDebug("SSL connection closed by peer: fd={Fd}", _connectionState.Fd);
                    break;
                }
                else
                {
                    // Negative = error (shouldn't happen with async API, but handle it)
                    _logger.LogError("SSL_read returned unexpected value: {BytesRead}", bytesRead);
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
            _logger.LogError(ex, "Error in SSL receive loop");
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
                                _logger.LogError("Peer closed connection during write");
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
            _logger.LogError(ex, "Error in SSL send loop");
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

        // Cancel pending reads to unblock the loops
        Application.Input.CancelPendingRead();
        Application.Output.CancelPendingFlush();
    }

    /// <summary>
    /// Called when the SSL connection encounters a fatal error (e.g., peer disconnect via EPOLLRDHUP).
    /// This triggers cleanup even if no read/write was pending.
    /// </summary>
    private void OnSslFatalError(Exception ex)
    {
        // Check if already disposed or aborted - connection may have been cleaned up already
        if (_aborted || Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        _logger.LogDebug(ex, "SSL fatal error for fd={Fd}, triggering disposal", _connectionState.Fd);
        
        // First abort to cancel pending operations
        Abort(new ConnectionAbortedException("SSL connection error", ex));
        
        // Queue async disposal on thread pool since we can't await here
        _ = Task.Run(async () =>
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception disposeEx)
            {
                _logger.LogDebug(disposeEx, "Error during disposal triggered by SSL fatal error");
            }
        });
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

        // 6. Graceful SSL shutdown and cleanup
        _connectionState.Dispose();
        _socket.Dispose();

        // 7. Signal connection closed
        _connectionClosedTokenSource.Cancel();
        _connectionClosedTokenSource.Dispose();
    }
}
