// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

internal class SocketConnectionListener : IConnectionListener
{
    private readonly SocketConnectionContextFactory _factory;
    protected readonly ILogger _logger;
    private Socket? _listenSocket;
    protected readonly SocketTransportOptions Options;

    public EndPoint EndPoint { get; private set; }

    internal SocketConnectionListener(
        EndPoint endpoint,
        SocketTransportOptions options,
        ILoggerFactory loggerFactory)
    {
        EndPoint = endpoint;
        Options = options;
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
            listenSocket = Options.CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint != null);
        EndPoint = listenSocket.LocalEndPoint;

        listenSocket.Listen(Options.Backlog);

        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");

                var acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = Options.NoDelay;
                }

                return CreateConnectionFromSocket(acceptSocket, _factory);
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
        }
    }

    /// <summary>
    /// Creates a connection from an accepted socket.
    /// This method can be overridden by derived classes to customize connection creation.
    /// </summary>
    protected virtual ConnectionContext CreateConnectionFromSocket(Socket socket, SocketConnectionContextFactory factory)
    {
        return factory.Create(socket);
    }

    /// <summary>
    /// Creates a connection from an accepted socket with direct customization support.
    /// This version provides access to all the factory internals for maximum flexibility.
    /// </summary>
    protected virtual ConnectionContext CreateConnectionFromSocket(
        Socket socket,
        MemoryPool<byte> memoryPool,
        PipeScheduler socketScheduler,
        SocketSenderPool socketSenderPool,
        PipeOptions inputOptions,
        PipeOptions outputOptions,
        ILogger logger)
    {
        // Base implementation: create standard SocketConnection
        return new SocketConnection(
            socket,
            memoryPool,
            socketScheduler,
            logger,
            socketSenderPool,
            inputOptions,
            outputOptions,
            waitForData: Options.WaitForDataBeforeAllocatingBuffer,
            finOnError: Options.FinOnError);
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
