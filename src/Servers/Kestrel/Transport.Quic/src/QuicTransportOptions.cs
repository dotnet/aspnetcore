// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;

/// <summary>
/// Options for Quic based connections.
/// </summary>
public class QuicTransportOptions
{
    /// <summary>
    /// The maximum number of concurrent bi-directional streams per connection.
    /// </summary>
    public ushort MaxBidirectionalStreamCount { get; set; } = 100;

    /// <summary>
    /// The maximum number of concurrent inbound uni-directional streams per connection.
    /// </summary>
    public ushort MaxUnidirectionalStreamCount { get; set; } = 10;

    /// <summary>
    /// Sets the idle timeout for connections and streams.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(130); // Matches KestrelServerLimits.KeepAliveTimeout.

    /// <summary>
    /// The maximum read size.
    /// </summary>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// The maximum write size.
    /// </summary>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// The maximum length of the pending connection queue.
    /// </summary>
    public int Backlog { get; set; } = 512;

    internal ISystemClock SystemClock = new SystemClock();
}
