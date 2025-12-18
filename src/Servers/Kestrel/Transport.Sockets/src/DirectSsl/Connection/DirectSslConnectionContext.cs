// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Connection context that uses native OpenSSL for TLS.
/// After handshake completes, owns the SSL pointer and uses SSL_read/SSL_write for data transfer.
/// </summary>
internal sealed unsafe class DirectSslConnection : TransportConnection
{
    private readonly Socket _socket;
    private readonly IntPtr _ssl;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly CancellationTokenSource _connectionClosedTokenSource = new();

    private Task? _receiveTask;
    private Task? _sendTask;
    private volatile bool _disposed;

    public DirectSslConnection(
        Socket socket,
        IntPtr ssl,
        EndPoint? localEndPoint,
        EndPoint? remoteEndPoint,
        MemoryPool<byte> memoryPool,
        ILogger logger)
    {
        _socket = socket;
        _ssl = ssl;
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
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_disposed)
            {
                var memory = Application.Output.GetMemory();

                int bytesRead;
                fixed (byte* ptr = memory.Span)
                {
                    // Blocking SSL_read - TODO: make non-blocking with epoll 
                    // This may be achieved by using same worker thread for read and write as well
                    bytesRead = NativeSsl.ssl_read(_ssl, ptr, memory.Length);
                }

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
                else if (bytesRead == -1)
                {
                    // Would block - shouldn't happen with blocking socket, but handle it
                    await Task.Delay(1);
                }
                else
                {
                    // Error
                    _logger.LogError("SSL_read error: {Error}", bytesRead);
                    error = new IOException($"SSL_read failed with error {bytesRead}");
                    break;
                }
            }
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
    /// </summary>
    private async Task SendLoopAsync()
    {
        Exception? error = null;

        try
        {
            while (!_disposed)
            {
                var result = await Application.Input.ReadAsync();
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    break;
                }

                try
                {
                    foreach (var segment in buffer)
                    {
                        var remaining = segment;
                        while (remaining.Length > 0)
                        {
                            int bytesWritten;
                            fixed (byte* ptr = remaining.Span)
                            {
                                // Blocking SSL_write - TODO: make non-blocking with epoll
                                bytesWritten = NativeSsl.ssl_write(_ssl, ptr, remaining.Length);
                            }

                            if (bytesWritten > 0)
                            {
                                remaining = remaining.Slice(bytesWritten);
                            }
                            else if (bytesWritten == -1)
                            {
                                // Would block - retry
                                await Task.Delay(1);
                            }
                            else
                            {
                                // Error
                                throw new IOException($"SSL_write failed with error {bytesWritten}");
                            }
                        }
                    }
                }
                finally
                {
                    Application.Input.AdvanceTo(buffer.End);
                }

                if (result.IsCompleted)
                {
                    break;
                }
            }
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
