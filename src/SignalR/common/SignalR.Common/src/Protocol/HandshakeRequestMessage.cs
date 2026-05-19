// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// A handshake request message.
/// </summary>
public class HandshakeRequestMessage : HubMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeRequestMessage"/> class.
    /// </summary>
    /// <param name="protocol">The requested protocol name.</param>
    /// <param name="version">The requested protocol version.</param>
    public HandshakeRequestMessage(string protocol, int version)
    {
        Protocol = protocol;
        Version = version;
    }

    /// <summary>
    /// Gets the requested protocol name.
    /// </summary>
    public string Protocol { get; }

    /// <summary>
    /// Gets the requested protocol version.
    /// </summary>
    public int Version { get; }
}
