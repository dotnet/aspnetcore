// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// A handshake response message.
/// </summary>
public class HandshakeResponseMessage : HubMessage
{
    /// <summary>
    /// An empty response message with no error.
    /// </summary>
    public static readonly HandshakeResponseMessage Empty = new HandshakeResponseMessage(error: null);

    /// <summary>
    /// Gets the optional error message.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandshakeResponseMessage"/> class.
    /// An error response does need a minor version. Since the handshake has failed, any extra data will be ignored.
    /// </summary>
    /// <param name="error">Error encountered by the server, indicating why the handshake has failed.</param>
    public HandshakeResponseMessage(string? error)
    {
        Error = error;
    }
}
