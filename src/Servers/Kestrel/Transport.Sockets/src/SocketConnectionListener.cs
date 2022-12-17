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

internal sealed class SocketConnectionListener : IConnectionListener, IConcurrentConnectionListener
{
    private readonly SocketConnectionContextFactory _factory;
    private readonly ILogger _logger;
    private Socket? _listenSocket;
    private readonly SocketTransportOptions _options;

    public EndPoint EndPoint { get; private set; }
    int IConcurrentConnectionListener.MaxAccepts => 1; // int.MaxValue; // not restricted

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

        _listenSocket = listenSocket;
    }

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");
                return Accept(await _listenSocket.AcceptAsync(cancellationToken));
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

    public async IAsyncEnumerable<object> AcceptManyAsync()
    {
        using var accepter = new SocketAccepter(PipeScheduler.Inline);
        var firstChunk = ArrayPool<byte>.Shared.Rent(1024); // allow payload in accept
        while (true)
        {
            SocketOperationResult result;
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");
                result = await accepter.AcceptAsync(_listenSocket, firstChunk);

                if (result.HasError) // the same as a thrown SocketException
                {
                    throw result.SocketError; // use existing exception handling
                }
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                break;
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                break;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                SocketsLog.ConnectionReset(_logger, connectionId: "(null)");
                continue;
            }

            if (result.BytesTransferred == 0)
            {
                yield return accepter.AcceptSocket!;
            }
            else
            {
                var payload = new ArraySegment<byte>(firstChunk, 0, result.BytesTransferred);
                yield return Tuple.Create(accepter.AcceptSocket!, payload); // no point just boxing a VT
                firstChunk = ArrayPool<byte>.Shared.Rent(1024); // rent a new chunk (Accept will release the old)
            }
        }
        ArrayPool<byte>.Shared.Return(firstChunk);
    }

    public ConnectionContext Accept(object token)
    {
        ArraySegment<byte> payload;
        Socket acceptSocket;

        switch (token)
        {
            case Socket socket:
                acceptSocket = socket;
                payload = default;
                break;
            case Tuple<Socket, ArraySegment<byte>> tuple:
                acceptSocket = tuple.Item1;
                payload = tuple.Item2;
                break;
            default:
                return null!; // nope
        }

        // Only apply no delay to Tcp based endpoints
        if (acceptSocket.LocalEndPoint is IPEndPoint)
        {
            acceptSocket.NoDelay = _options.NoDelay;
        }

        var connection = _factory.CreateUnstartedSocketConnection(acceptSocket);
        var received = payload.Count;
        if (received != 0)
        {
            connection.Input.Write(payload.AsSpan());
            ArrayPool<byte>.Shared.Return(payload.Array!);
        }
        connection.Start(flushImmediately: received != 0);
        return connection;
    }
}
