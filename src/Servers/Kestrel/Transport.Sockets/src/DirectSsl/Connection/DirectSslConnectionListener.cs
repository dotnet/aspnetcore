// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

internal sealed class DirectSslConnectionListener : IConnectionListener
{
    private readonly ILogger _logger;
    private readonly DirectSslTransportOptions _options;

    private readonly DirectSslConnectionContextFactory _factory;
    private readonly MemoryPool<byte> _memoryPool;

    private readonly SslContext _sslContext;
    private readonly SslEventPumpPool _pumpPool;

    private Socket? _listenSocket;

    // Channel for connections that have completed handshake and are ready to be returned
    private readonly Channel<DirectSslConnection> _readyConnections;

    // Track pending handshakes so we can cancel them on dispose
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _pendingHandshakes = new();

    // Ensure only one accept loop runs
    private Task? _acceptLoopTask;
    private readonly object _acceptLoopLock = new();
    private CancellationTokenSource? _acceptLoopCts;

    public EndPoint EndPoint { get; private set; }

    public DirectSslConnectionListener(
        ILoggerFactory loggerFactory,
        SslContext sslContext,
        SslEventPumpPool pumpPool,
        EndPoint endpoint,
        DirectSslTransportOptions options,
        MemoryPool<byte> memoryPool)
    {
        _logger = loggerFactory.CreateLogger<DirectSslConnectionListener>();
        _options = options;
        _memoryPool = memoryPool;

        _pumpPool = pumpPool;
        _sslContext = sslContext;
        EndPoint = endpoint;

        _factory = new(loggerFactory, _sslContext, memoryPool);

        // Unbounded channel - handshakes complete asynchronously and we don't want to block them
        _readyConnections = Channel.CreateUnbounded<DirectSslConnection>(new UnboundedChannelOptions
        {
            SingleReader = false,  // Multiple AcceptAsync callers possible
            SingleWriter = false,  // Multiple handshakes complete concurrently
        });
    }

    internal void Bind()
    {
        if (_listenSocket is not null)
        {
            throw new InvalidOperationException(SocketsStrings.TransportAlreadyBound);
        }

        Socket listenSocket;
        try
        {
            listenSocket = _options.CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint != null);
        EndPoint = listenSocket.LocalEndPoint;

        // Set TCP_DEFER_ACCEPT - kernel waits for client data before completing accept()
        // This guarantees ClientHello is in buffer when AcceptAsync returns (for TLS handshake)
        if (OperatingSystem.IsLinux())
        {
            int fd = (int)listenSocket.Handle;
            int timeout = 1;  // seconds
            int result = NativeLibc.SetSocketOption(fd, ref timeout, sizeof(int));
            if (result < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                _logger?.LogWarning("Failed to set TCP_DEFER_ACCEPT: errno={Errno}", errno);
                // Don't throw - it's an optimization, not required
            }
            else
            {
                _logger?.LogDebug("TCP_DEFER_ACCEPT enabled on listening socket");
            }
        }

        listenSocket.Listen(_options.Backlog);
        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        // Start the accept loop if not already running (first caller wins)
        EnsureAcceptLoopStarted();

        try
        {
            // Wait for a connection that has completed handshake
            return await _readyConnections.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            // Channel closed during shutdown
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private void EnsureAcceptLoopStarted()
    {
        if (_acceptLoopTask is not null)
        {
            return;
        }

        lock (_acceptLoopLock)
        {
            if (_acceptLoopTask is not null)
            {
                return;
            }

            _acceptLoopCts = new CancellationTokenSource();
            _acceptLoopTask = AcceptLoopAsync(_acceptLoopCts.Token);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Debug.Assert(_listenSocket is not null, "Bind must be called first.");

                var acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = _options.NoDelay;
                }

                // Fire-and-forget the handshake - don't block the accept loop!
                _ = HandshakeAndEnqueueAsync(acceptSocket);
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync - close the channel and exit
                _readyConnections.Writer.TryComplete();
                return;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync - close the channel and exit
                _readyConnections.Writer.TryComplete();
                return;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                SocketsLog.ConnectionReset(_logger, connectionId: "(null)");
            }
        }

        // Cancellation was requested - complete the channel
        _readyConnections.Writer.TryComplete();
    }

    private async Task HandshakeAndEnqueueAsync(Socket acceptSocket)
    {
        var fd = (int)acceptSocket.Handle;
        
        // Use a separate CTS for the handshake - not linked to the accept loop cancellation.
        // This allows in-flight handshakes to complete during graceful shutdown.
        // The handshake CTS is tracked in _pendingHandshakes and cancelled explicitly in Dispose.
        var cts = new CancellationTokenSource();
        _pendingHandshakes[fd] = cts;

        try
        {
            var connection = await _factory.CreateAsync(_pumpPool, acceptSocket, cts.Token);

            if (connection is not null)
            {
                // Handshake succeeded - enqueue for AcceptAsync to return
                // Use CancellationToken.None since channel completion signals shutdown
                if (_readyConnections.Writer.TryWrite(connection))
                {
                    return;
                }
                
                // Channel is completed (shutting down) - dispose the connection
                await connection.DisposeAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Handshake was cancelled (shutdown or timeout)
            acceptSocket.Dispose();
        }
        catch (SslException ex)
        {
            SocketsLog.SslHandshakeFailed(_logger, connectionId: "(null)", ex);
            acceptSocket.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Handshake failed for fd={Fd}", fd);
            acceptSocket.Dispose();
        }
        finally
        {
            _pendingHandshakes.TryRemove(fd, out _);
            cts.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _acceptLoopCts?.Cancel();
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();

        // Cancel all pending handshakes
        foreach (var cts in _pendingHandshakes.Values)
        {
            cts.Cancel();
        }

        // Wait for the accept loop to finish before disposing the CTS
        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch
            {
                // Ignore exceptions - we're disposing
            }
        }

        _acceptLoopCts?.Dispose();
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _acceptLoopCts?.Cancel();
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();

        // Cancel all pending handshakes
        foreach (var cts in _pendingHandshakes.Values)
        {
            cts.Cancel();
        }

        // Wait for the accept loop to finish
        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask;
            }
            catch
            {
                // Ignore exceptions - we're unbinding
            }
        }
    }
}
