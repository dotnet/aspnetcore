// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Describes the current state of the <see cref="HubConnection"/> to the server.
/// </summary>
public enum HubConnectionState
{
    /// <summary>
    /// The hub connection is disconnected.
    /// </summary>
    Disconnected,
    /// <summary>
    /// The hub connection is connected.
    /// </summary>
    Connected,
    /// <summary>
    /// The hub connection is connecting.
    /// </summary>
    Connecting,
    /// <summary>
    /// The hub connection is reconnecting.
    /// </summary>
    Reconnecting,
}
