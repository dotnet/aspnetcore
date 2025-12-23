// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
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

        listenSocket.Listen(_options.Backlog);

        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");

                var acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = _options.NoDelay;
                }

                var connection = await _factory.CreateAsync(_pumpPool, acceptSocket, cancellationToken);

                // If handshake failed, continue accepting other connections
                if (connection is null)
                {
                    continue;
                }
                
                return connection;
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                SocketsLog.ConnectionReset(_logger, connectionId: "(null)");
            }
            catch (SslException ex)
            {
                SocketsLog.SslHandshakeFailed(_logger, connectionId: "(null)", ex);
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _listenSocket?.Dispose();
        return default;
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        return default;
    }
}
