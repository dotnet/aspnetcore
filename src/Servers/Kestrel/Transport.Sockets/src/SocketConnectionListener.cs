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

internal sealed class SocketConnectionListener : IConnectionListener //, IConcurrentConnectionListener
{
    private readonly SocketConnectionContextFactory _factory;
    private readonly ILogger _logger;
    private Socket? _listenSocket;
    private readonly SocketTransportOptions _options;

    public EndPoint EndPoint { get; private set; }
    // int IConcurrentConnectionListener.MaxAccepts => int.MaxValue; // not restricted

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

    private readonly SocketAccepter _accepter = new(PipeScheduler.Inline);

    public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        => default; // nope

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public async ValueTask<ConnectionContext?> AcceptAsync2(CancellationToken cancellationToken = default)
    {
        var firstChunk = ArrayPool<byte>.Shared.Rent(2048);
        if (firstChunk is not null)
        {
            throw new InvalidOperationException("huh");
        }
        while (true)
        {
            try
            {
                Debug.Assert(_listenSocket != null, "Bind must be called first.");

                var result = await _accepter.AcceptAsync(_listenSocket, firstChunk);
                if (result.HasError) // the same as a thrown SocketException
                {
                    throw result.SocketError; // use existing exception handling
                }
                var acceptSocket = _accepter.AcceptSocket!;

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                {
                    acceptSocket.NoDelay = _options.NoDelay;
                }

                var connection = _factory.CreateUnstartedSocketConnection(acceptSocket);
                var received = result.BytesTransferred;
                Console.WriteLine($"got {received} bytes in accept");
                if (received != 0)
                {
                    new ReadOnlyMemory<byte>(firstChunk, 0, received).CopyTo(connection.Input.GetMemory(received));
                    connection.Input.Advance(received);
                }
                connection.Start(flushImmediately: received != 0);
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
            finally
            {
                ArrayPool<byte>.Shared.Return(firstChunk!);
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
        _accepter?.Dispose();
        _factory.Dispose();

        return default;
    }
}
