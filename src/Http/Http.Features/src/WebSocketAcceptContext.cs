// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A context for negotiating a websocket upgrade.
/// </summary>
public class WebSocketAcceptContext
{
    private int _serverMaxWindowBits = 15;

    /// <summary>
    /// Gets or sets the subprotocol being negotiated.
    /// </summary>
    public virtual string? SubProtocol { get; set; }

    /// <summary>
    /// The interval to send pong frames. This is a heart-beat that keeps the connection alive.
    /// </summary>
    public virtual TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// Enables support for the 'permessage-deflate' WebSocket extension.<para />
    /// Be aware that enabling compression over encrypted connections makes the application subject to CRIME/BREACH type attacks.
    /// It is strongly advised to turn off compression when sending data containing secrets by
    /// specifying <see cref="WebSocketMessageFlags.DisableCompression"/> when sending such messages.
    /// </summary>
    public bool DangerousEnableCompression { get; set; }

    /// <summary>
    /// Disables server context takeover when using compression.
    /// This setting reduces the memory overhead of compression at the cost of a potentially worse compression ratio.
    /// </summary>
    /// <remarks>
    /// This property does nothing when <see cref="DangerousEnableCompression"/> is false,
    /// or when the client does not use compression.
    /// </remarks>
    /// <value>
    /// false
    /// </value>
    public bool DisableServerContextTakeover { get; set; }

    /// <summary>
    /// Sets the maximum base-2 logarithm of the LZ77 sliding window size that can be used for compression.
    /// This setting reduces the memory overhead of compression at the cost of a potentially worse compression ratio.
    /// </summary>
    /// <remarks>
    /// This property does nothing when <see cref="DangerousEnableCompression"/> is false,
    /// or when the client does not use compression.
    /// Valid values are 9 through 15.
    /// </remarks>
    /// <value>
    /// 15
    /// </value>
    public int ServerMaxWindowBits
    {
        get => _serverMaxWindowBits;
        set
        {
            if (value < 9 || value > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(ServerMaxWindowBits),
                    "The argument must be a value from 9 to 15.");
            }
            _serverMaxWindowBits = value;
        }
    }
}
