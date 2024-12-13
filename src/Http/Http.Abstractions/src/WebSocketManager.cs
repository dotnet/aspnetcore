// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Manages the establishment of WebSocket connections for a specific HTTP request.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(WebSocketManagerDebugView))]
public abstract class WebSocketManager
{
    /// <summary>
    /// Gets a value indicating whether the request is a WebSocket establishment request.
    /// </summary>
    public abstract bool IsWebSocketRequest { get; }

    /// <summary>
    /// Gets the list of requested WebSocket sub-protocols.
    /// </summary>
    public abstract IList<string> WebSocketRequestedProtocols { get; }

    /// <summary>
    /// Transitions the request to a WebSocket connection.
    /// </summary>
    /// <returns>A task representing the completion of the transition.</returns>
    public virtual Task<WebSocket> AcceptWebSocketAsync()
    {
        return AcceptWebSocketAsync(subProtocol: null);
    }

    /// <summary>
    /// Transitions the request to a WebSocket connection using the specified sub-protocol.
    /// </summary>
    /// <param name="subProtocol">The sub-protocol to use.</param>
    /// <returns>A task representing the completion of the transition.</returns>
    public abstract Task<WebSocket> AcceptWebSocketAsync(string? subProtocol);

    /// <summary>
    ///
    /// </summary>
    /// <param name="acceptContext"></param>
    /// <returns></returns>
    public virtual Task<WebSocket> AcceptWebSocketAsync(WebSocketAcceptContext acceptContext) => throw new NotImplementedException();

    private string DebuggerToString()
    {
        return IsWebSocketRequest switch
        {
            false => "IsWebSocketRequest = false",
            true => $"IsWebSocketRequest = true, RequestedProtocols = {string.Join(',', WebSocketRequestedProtocols)}",
        };
    }

    private sealed class WebSocketManagerDebugView(WebSocketManager manager)
    {
        private readonly WebSocketManager _manager = manager;

        public bool IsWebSocketRequest => _manager.IsWebSocketRequest;
        public IList<string> WebSocketRequestedProtocols => new List<string>(_manager.WebSocketRequestedProtocols);
    }
}
