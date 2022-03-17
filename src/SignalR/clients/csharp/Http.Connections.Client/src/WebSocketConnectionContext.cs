// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Http.Connections.Client;

/// <summary>
/// Used to make a connection to an SignalR using a WebSocket-based transport.
/// </summary>
public sealed class WebSocketConnectionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketConnectionContext"/> class.
    /// </summary>
    /// <param name="uri">The URL to connect to.</param>
    /// <param name="options">The connection options to use.</param>
    public WebSocketConnectionContext(Uri uri, HttpConnectionOptions options)
    {
        Uri = uri;
        Options = options;
    }

    /// <summary>
    /// Gets the URL to connect to.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets the connection options to use.
    /// </summary>
    public HttpConnectionOptions Options { get; }
}
