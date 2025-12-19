// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Connection context that uses native OpenSSL for TLS - nginx-style.
/// 
/// After handshake completes:
/// - Owns the SSL pointer
/// - Uses the assigned SslWorker for all I/O (read/write via epoll)
/// - All I/O happens on the worker's thread for maximum performance
/// </summary>
internal sealed class DirectSslConnection : TransportConnection
{
    private readonly Socket _socket;
    private readonly IntPtr _ssl;
    private readonly int _clientFd;
    private readonly SslWorker _worker;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new();

    private Task? _receiveTask;
    private Task? _sendTask;
    private volatile bool _disposed;

    public DirectSslConnection(
        Socket socket,
        IntPtr ssl,
        SslWorker worker,
        EndPoint? localEndPoint,
        EndPoint? remoteEndPoint,
        MemoryPool<byte> memoryPool,
        ILogger logger)
    {
        _socket = socket;
        _ssl = ssl;
        _clientFd = (int)socket.Handle;
        _worker = worker;
        _memoryPool = memoryPool;
        _logger = logger;

        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
        ConnectionClosed = _connectionClosedTokenSource.Token;

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
    /// Uses the worker's epoll-based async SSL_read.
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_disposed)
            {
                var memory = Application.Output.GetMemory();

                // Use worker's async SSL_read (goes through epoll event loop)
                int bytesRead = await _worker.SslReadAsync(_ssl, _clientFd, memory);

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
                    _logger.LogDebug("SSL connection closed by peer");
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
    /// Uses the worker's epoll-based async SSL_write.
    /// </summary>
    private async Task SendLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_disposed)
            {
                var result = await Application.Input.ReadAsync();
                
                // Check for cancellation first
                if (result.IsCanceled)
                {
                    break;
                }
                
                var buffer = result.Buffer;

                // Process buffer data BEFORE checking IsCompleted
                // This ensures the final chunk (e.g., "0\r\n\r\n" terminator) is sent
                if (!buffer.IsEmpty)
                {
                    foreach (var segment in buffer)
                    {
                        if (segment.Length > 0)
                        {
                            // Use worker's async SSL_write (goes through epoll event loop)
                            // Worker handles partial writes internally
                            // segment is ReadOnlyMemory<byte> - no copy needed!
                            await _worker.SslWriteAsync(_ssl, _clientFd, segment, segment.Length);
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
        _connectionClosedTokenSource.Cancel();
        Application.Input.CancelPendingRead();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        Transport.Input.Complete();
        Transport.Output.Complete();

        // Wait for loops to finish
        if (_receiveTask != null)
        {
            await _receiveTask;
        }
        if (_sendTask != null)
        {
            await _sendTask;
        }

        // SSL shutdown and cleanup
        try
        {
            NativeSsl.ssl_connection_destroy(_ssl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during SSL shutdown");
        }

        // Close socket
        _socket.Dispose();

        _connectionClosedTokenSource.Cancel();
        _connectionClosedTokenSource.Dispose();
    }
}
