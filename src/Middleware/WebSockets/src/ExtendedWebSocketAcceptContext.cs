// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebSockets;

/// <summary>
/// Extends the <see cref="WebSocketAcceptContext"/> class with additional properties.
/// </summary>
[Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is Microsoft.AspNetCore.Http.WebSocketAcceptContext.")]
public class ExtendedWebSocketAcceptContext : WebSocketAcceptContext
{
    /// <inheritdoc />
    public override string? SubProtocol { get; set; }

    /// <summary>
    /// This property is obsolete and has no effect.
    /// </summary>
    [Obsolete("Setting this property has no effect. It will be removed in a future version.")]
    public int? ReceiveBufferSize { get; set; }

    /// <summary>
    /// The interval to send pong frames. This is a heart-beat that keeps the connection alive.
    /// </summary>
    public new TimeSpan? KeepAliveInterval { get; set; }
}
