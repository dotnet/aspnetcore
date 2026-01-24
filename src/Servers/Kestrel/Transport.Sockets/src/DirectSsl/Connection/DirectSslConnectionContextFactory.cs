// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// Factory for creating <see cref="DirectSslConnection"/> instances.
/// Performs TLS handshake and assigns connection to an event pump.
/// </summary>
internal sealed class DirectSslConnectionContextFactory : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly SslContext _sslContext;
    
    // Cached loggers - reused for all connections to avoid per-connection allocations
    private readonly ILogger<SslConnectionState> _sslConnectionStateLogger;
    private readonly ILogger<DirectSslConnection> _directSslConnectionLogger;

    public DirectSslConnectionContextFactory(
        ILoggerFactory loggerFactory,
        SslContext sslContext,
        MemoryPool<byte> memoryPool)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DirectSslConnectionContextFactory>();
        _memoryPool = memoryPool;
        _sslContext = sslContext;
        
        // Create loggers once and reuse for all connections
        _sslConnectionStateLogger = loggerFactory.CreateLogger<SslConnectionState>();
        _directSslConnectionLogger = loggerFactory.CreateLogger<DirectSslConnection>();
    }

    public async ValueTask<DirectSslConnection?> CreateAsync(
        SslEventPumpPool pumpPool,
        Socket acceptSocket,
        CancellationToken cancellationToken = default)
    {
        int fd = (int)acceptSocket.Handle;
        NativeSsl.SetNonBlocking(fd);
        
        // Set TCP_NODELAY to disable Nagle's algorithm for lower latency
        acceptSocket.NoDelay = true;
        
        // Set SO_LINGER to ensure graceful close with timeout
        // This prevents RST being sent when there's data in the send buffer
        acceptSocket.LingerState = new LingerOption(true, 5);  // linger for up to 5 seconds

        // 3. Create SSL and bind to socket
        IntPtr ssl = NativeSsl.SSL_new(_sslContext.Handle);
        if (ssl == IntPtr.Zero)
        {
            acceptSocket.Dispose();
            throw new SslException("Failed to create SSL object.");
        }

        NativeSsl.SSL_set_fd(ssl, fd);
        NativeSsl.SSL_set_accept_state(ssl);

        // 4. Create connection state (use cached logger)
        var connectionState = new SslConnectionState(fd, ssl, _sslConnectionStateLogger);

        // 5. Get pump for this connection
        var pump = pumpPool.GetNextPump();

        // 6. Perform async handshake
        // IMPORTANT: Start handshake BEFORE registering with pump.
        // This ensures _handshakeTcs is set before any epoll events can arrive.
        // With edge-triggered epoll, EPOLLIN fires immediately on registration
        // if data is already available (e.g., ClientHello from client).
        try
        {
            _logger.LogDebug($"Initiating handshake for fd={connectionState.Fd}, ssl={connectionState.Ssl}");
            
            // Try initial handshake - this will set _handshakeTcs if it needs to wait
            var handshakeTask = connectionState.HandshakeAsync();
            
            // Now register with pump - safe because _handshakeTcs is already set if needed
            pump.Register(connectionState);
            
            // Wait for handshake to complete
            await handshakeTask;

            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (Exception ex)
        {
            pump.Unregister(fd);
            connectionState.Dispose();
            acceptSocket.Dispose();
            throw new SslException($"Failed to perform handshake: fd={fd}", ex);
        }

        // Note: This legacy path still uses Socket wrapper but is not the main accept path
        // The main path uses SslEventPump.AcceptConnections() which avoids Socket wrapper entirely
        var connection = new DirectSslConnection(
            fd,                       // Pass fd directly
            connectionState,
            pump,
            acceptSocket.LocalEndPoint,
            acceptSocket.RemoteEndPoint,
            _memoryPool,
            _directSslConnectionLogger);  // Use cached logger
            
        connection.Start();
        
        // Note: In legacy path, acceptSocket is not disposed here - DirectSslConnection will
        // close the fd via P/Invoke in DisposeAsync. However, the Socket object won't be
        // properly cleaned up. This is acceptable since this path is not the main accept flow.
        return connection;
    }

    public void Dispose()
    {
        // MemoryPool is owned by caller
    }
}
