// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Connection;

/// <summary>
/// DirectTls connection listener that uses EPOLLEXCLUSIVE for worker-based accept.
/// Each pump thread accepts connections directly in its epoll loop, eliminating
/// the accept thread bottleneck and cross-thread handoff overhead.
/// </summary>
internal sealed class DirectTlsConnectionListener : IConnectionListener
{
    private readonly ILogger? _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DirectTlsTransportOptions _options;

    private readonly MemoryPool<byte> _memoryPool;

    private readonly TlsContext _tlsContext;
    private readonly Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? _contextResolver;
    private readonly TlsEventPumpPool _pumpPool;
    private readonly Action<ConnectionContext, ReadOnlySequence<byte>>? _clientHelloCallback;

    private Socket? _listenSocket;

    // Channel for connections that have completed handshake and are ready to be returned
    private readonly Channel<DirectTlsConnection> _readyConnections;

    public EndPoint EndPoint { get; private set; }

    public DirectTlsConnectionListener(
        ILoggerFactory loggerFactory,
        TlsContext tlsContext,
        Func<ConnectionContext?, string?, (TlsContext Context, RemoteCertificateValidationCallback? ClientCertificateValidation)>? contextResolver,
        TlsEventPumpPool pumpPool,
        EndPoint endpoint,
        DirectTlsTransportOptions options,
        MemoryPool<byte> memoryPool,
        Action<ConnectionContext, ReadOnlySequence<byte>>? clientHelloCallback = null)
    {
        ArgumentNullException.ThrowIfNull(tlsContext);

        _logger = loggerFactory.CreateLogger<DirectTlsConnectionListener>();
        _loggerFactory = loggerFactory;
        _options = options;
        _memoryPool = memoryPool;

        _pumpPool = pumpPool;
        _tlsContext = tlsContext;
        _contextResolver = contextResolver;
        _clientHelloCallback = clientHelloCallback;
        EndPoint = endpoint;

        // Unbounded channel - handshakes complete asynchronously and we don't want to block them
        _readyConnections = Channel.CreateUnbounded<DirectTlsConnection>(new UnboundedChannelOptions
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

        listenSocket.Listen(_options.Backlog);

        // Set listen socket to non-blocking for the EPOLLEXCLUSIVE accept pattern.
        // Each pump waits on epoll and calls Socket.Accept(); non-blocking mode makes
        // a drained accept throw SocketError.WouldBlock instead of blocking the pump.
        listenSocket.Blocking = false;

        _listenSocket = listenSocket;
        
        // Start all pump threads with the listen socket (EPOLLEXCLUSIVE)
        // Each pump will accept connections directly in its epoll loop
        int listenFd = (int)listenSocket.Handle;
        _pumpPool.StartWithListenSocket(
            listenFd, 
            _tlsContext, 
            _contextResolver,
            _readyConnections.Writer, 
            _memoryPool,
            _options.NoDelay,
            _clientHelloCallback);
            
        _logger?.LogInformation("DirectTls listener started with EPOLLEXCLUSIVE worker accept");
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

        // This listener owns its pump pool; stop the pump threads and release their epoll fds.
        _pumpPool.Dispose();
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        _readyConnections.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
