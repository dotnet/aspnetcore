// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// A keep-alive message to let the other side of the connection know that the connection is still alive.
/// </summary>
public class PingMessage : HubMessage
{
    /// <summary>
    /// A static instance of the PingMessage to remove unneeded allocations.
    /// </summary>
    public static readonly PingMessage Instance = new PingMessage();

    private PingMessage()
    {
    }
}
