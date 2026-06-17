// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Options used to configure <see cref="HubConnectionContext"/>.
/// </summary>
public class HubConnectionContextOptions
{
    /// <summary>
    /// Gets or sets the interval used to send keep alive pings to connected clients.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the time window clients have to send a message before the server closes the connection.
    /// </summary>
    public TimeSpan ClientTimeoutInterval { get; set; }

    /// <summary>
    /// Gets or sets the max buffer size for client upload streams.
    /// </summary>
    public int StreamBufferCapacity { get; set; }

    /// <summary>
    /// Gets or sets the maximum message size the client can send.
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; }

    internal TimeProvider TimeProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the maximum parallel hub method invocations.
    /// </summary>
    public int MaximumParallelInvocations { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum bytes to buffer per connection when using stateful reconnect.
    /// </summary>
    internal long StatefulReconnectBufferSize { get; set; } = 100_000;
}
