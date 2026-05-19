// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Configures options for the <see cref="HubConnection" />.
/// </summary>
public sealed class HubConnectionOptions
{
    /// <summary>
    /// Configures ServerTimeout for the <see cref="HubConnection" />.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = HubConnection.DefaultServerTimeout;

    /// <summary>
    /// Configures KeepAliveInterval for the <see cref="HubConnection" />.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = HubConnection.DefaultKeepAliveInterval;

    /// <summary>
    /// Amount of serialized messages in bytes we'll buffer when using Stateful Reconnect until applying backpressure to sends from the client.
    /// </summary>
    /// <remarks>Defaults to 100,000 bytes.</remarks>
    public long StatefulReconnectBufferSize { get; set; } = HubConnection.DefaultStatefulReconnectBufferSize;

}
