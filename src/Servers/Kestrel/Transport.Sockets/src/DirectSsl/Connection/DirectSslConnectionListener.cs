// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Interop;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// DirectSsl connection listener that uses EPOLLEXCLUSIVE for worker-based accept.
/// Each pump thread accepts connections directly in its epoll loop, eliminating
/// the accept thread bottleneck and cross-thread handoff overhead.
/// </summary>
internal sealed class DirectSslConnectionListener : IConnectionListener
{
    private readonly ILogger? _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DirectSslTransportOptions _options;

    private readonly MemoryPool<byte> _memoryPool;

    private readonly SslContext _sslContext;
    private readonly SslEventPumpPool _pumpPool;

    private Socket? _listenSocket;

    // Channel for connections that have completed handshake and are ready to be returned
    private readonly Channel<DirectSslConnection> _readyConnections;

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
        _loggerFactory = loggerFactory;
        _options = options;
        _memoryPool = memoryPool;

        _pumpPool = pumpPool;
        _sslContext = sslContext;
        EndPoint = endpoint;

        // Unbounded channel - handshakes complete asynchronously and we don't want to block them
        _readyConnections = Channel.CreateUnbounded<DirectSslConnection>(new UnboundedChannelOptions
        {
            SingleReader = false,  // Multiple AcceptAsync callers possible
            SingleWriter = false,  // Multiple pump threads write concurrently
        });
    }

    internal void Bind()
    {
        if (_listenSocket is not null)
        {
            throw new InvalidOperationException("Transport already bound");
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
        
        // Set listen socket to non-blocking for EPOLLEXCLUSIVE accept pattern
        // Without this, accept4() blocks instead of returning EAGAIN
        if (OperatingSystem.IsLinux())
        {
            int fd = (int)listenSocket.Handle;
            NativeSsl.SetNonBlocking(fd);
            _logger?.LogDebug("Listen socket set to non-blocking mode");
        }
        
        _listenSocket = listenSocket;
        
        // Start all pump threads with the listen socket (EPOLLEXCLUSIVE)
        // Each pump will accept connections directly in its epoll loop
        int listenFd = (int)listenSocket.Handle;
        _pumpPool.StartWithListenSocket(
            listenFd, 
            _sslContext, 
            _readyConnections.Writer, 
            _memoryPool,
            _options.NoDelay);
            
        _logger?.LogInformation("DirectSsl listener started with EPOLLEXCLUSIVE worker accept");
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
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

    public async ValueTask DisposeAsync()
    {
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();

        // Drain any remaining connections from the channel
        while (_readyConnections.Reader.TryRead(out var connection))
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
