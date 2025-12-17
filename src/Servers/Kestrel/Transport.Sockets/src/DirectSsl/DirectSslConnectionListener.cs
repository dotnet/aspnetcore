// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal sealed class DirectSslConnectionListener : IConnectionListener
{
    private readonly ILogger _logger;
    private readonly SslContext _sslContext;
    
    private Socket? _listenSocket;

    public EndPoint EndPoint { get; private set; }

    public DirectSslConnectionListener(ILoggerFactory loggerFactory, SslContext sslContext, EndPoint endpoint)
    {
        _logger = loggerFactory.CreateLogger<DirectSslConnectionListener>();
        
        _sslContext = sslContext;
        EndPoint = endpoint;
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

    public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
