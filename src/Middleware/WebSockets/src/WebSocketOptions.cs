// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configuration options for the WebSocketMiddleware.
/// </summary>
public class WebSocketOptions
{
    private TimeSpan _keepAliveTimeout = Timeout.InfiniteTimeSpan;

    /// <summary>
    /// Constructs the <see cref="WebSocketOptions"/> class with default values.
    /// </summary>
    public WebSocketOptions()
    {
        KeepAliveInterval = TimeSpan.FromMinutes(2);
        AllowedOrigins = new List<string>();
    }

    /// <summary>
    /// The interval to send keep-alive frames. This is a heart-beat that keeps the connection alive.
    /// The default is two minutes.
    /// </summary>
    /// <remarks>
    /// May be either a Ping or a Pong frame, depending on if <see cref="KeepAliveTimeout" /> is set.
    /// </remarks>
    public TimeSpan KeepAliveInterval { get; set; }

    /// <summary>
    /// The time to wait for a Pong frame response after sending a Ping frame. If the time is exceeded the websocket will be aborted.
    /// </summary>
    /// <remarks>
    /// Default value is <see cref="Timeout.InfiniteTimeSpan"/>.
    /// <see cref="Timeout.InfiniteTimeSpan"/> and <see cref="TimeSpan.Zero"/> will disable the timeout.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="TimeSpan"/> is less than <see cref="TimeSpan.Zero"/>.
    /// </exception>
    public TimeSpan KeepAliveTimeout
    {
        get
        {
            return _keepAliveTimeout;
        }
        set
        {
            if (value != Timeout.InfiniteTimeSpan)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            }
            _keepAliveTimeout = value;
        }
    }

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
