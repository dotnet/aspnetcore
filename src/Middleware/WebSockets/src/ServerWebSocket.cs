// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebSockets;

/// <summary>
/// Used in ASP.NET Core to wrap a WebSocket with its associated HttpContext so that when the WebSocket is aborted
/// the underlying HttpContext is aborted. All other methods are delegated to the underlying WebSocket.
/// </summary>
internal sealed class ServerWebSocket : WebSocket
{
    private readonly WebSocket _wrappedSocket;
    private readonly HttpContext _context;

    internal ServerWebSocket(WebSocket wrappedSocket, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(wrappedSocket);
        ArgumentNullException.ThrowIfNull(context);

        _wrappedSocket = wrappedSocket;
        _context = context;
    }

    public override WebSocketCloseStatus? CloseStatus => _wrappedSocket.CloseStatus;

    public override string? CloseStatusDescription => _wrappedSocket.CloseStatusDescription;

    public override WebSocketState State => _wrappedSocket.State;

    public override string? SubProtocol => _wrappedSocket.SubProtocol;

    public override void Abort()
    {
        _wrappedSocket.Abort();
        _context.Abort();
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return _wrappedSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return _wrappedSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
    }

    public override void Dispose()
    {
        _wrappedSocket.Dispose();
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        return _wrappedSocket.ReceiveAsync(buffer, cancellationToken);
    }

    public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return _wrappedSocket.ReceiveAsync(buffer, cancellationToken);
    }

    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        return _wrappedSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }

    public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, WebSocketMessageFlags messageFlags, CancellationToken cancellationToken)
    {
        return _wrappedSocket.SendAsync(buffer, messageType, messageFlags, cancellationToken);
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        return _wrappedSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }
}
