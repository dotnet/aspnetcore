// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

internal sealed class SocketConnectionListener : IConnectionListener
{
    private readonly SocketConnectionContextFactory _factory;
    private readonly ILogger _logger;
    private Socket? _listenSocket;
    private readonly SocketTransportOptions _options;

    public EndPoint EndPoint { get; private set; }

    internal SocketConnectionListener(
        EndPoint endpoint,
        SocketTransportOptions options,
        ILoggerFactory loggerFactory)
    {
        EndPoint = endpoint;
        _options = options;
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
        _logger = logger;
        _factory = new SocketConnectionContextFactory(new SocketConnectionFactoryOptions(options), logger);
    }

    internal void Bind()
    {
        if (_listenSocket != null)
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

        // try to connect to ourselves to see if socket re-use is enabled
        using var args = SimpleSocketAwaitableEventArgs.GetArgs();
        
        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        Debug.Assert(_listenSocket != null, "Bind must be called first.");

        using var args = SimpleSocketAwaitableEventArgs.GetArgs(); // note: simple pool implementation
        bool reuseSocket = false;
        Socket? client = null;
        while (true)
        {
            try
            {
                args.AcceptSocket = client = reuseSocket ? SimpleSocketAwaitableEventArgs.TryGetSocket() : null;
                await args.AcceptAsync(_listenSocket);
                var acceptSocket = args.AcceptSocket!;
                SimpleSocketAwaitableEventArgs.OnAccepted();

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = _options.NoDelay;
                }

                return _factory.Create(acceptSocket);
            }
            catch (PlatformNotSupportedException ex) when (reuseSocket && client is not null)
            {
                Console.WriteLine(ex.Message);
                // AcceptAsync doesn't allow pre-existing socket in all scenarios
                reuseSocket = false;
                client?.Dispose();
                continue; // retry, now without re-use
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                Console.WriteLine(e.Message);
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null;
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
                // The connection got reset while it was in the backlog, so we try again.
                SocketsLog.ConnectionReset(_logger, connectionId: "(null)");
            }
        }
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        return default;
    }

    public ValueTask DisposeAsync()
    {
        _listenSocket?.Dispose();

        _factory.Dispose();

        return default;
    }
}
