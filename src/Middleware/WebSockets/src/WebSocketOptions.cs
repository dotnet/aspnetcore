// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configuration options for the WebSocketMiddleware.
/// </summary>
public class WebSocketOptions
{
    /// <summary>
    /// Constructs the <see cref="WebSocketOptions"/> class with default values.
    /// </summary>
    public WebSocketOptions()
    {
        KeepAliveInterval = TimeSpan.FromMinutes(2);
        AllowedOrigins = new List<string>();
    }

    /// <summary>
    /// Gets or sets the frequency at which to send Ping/Pong keep-alive control frames.
    /// The default is two minutes.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the size of the protocol buffer used to receive and parse frames.
    /// The default is 4kb.
    /// </summary>
    [Obsolete("Setting this property has no effect. It will be removed in a future version.")]
    public int ReceiveBufferSize { get; set; }

    /// <summary>
    /// Set the Origin header values allowed for WebSocket requests to prevent Cross-Site WebSocket Hijacking.
    /// By default all Origins are allowed.
    /// </summary>
    public IList<string> AllowedOrigins { get; }
}
