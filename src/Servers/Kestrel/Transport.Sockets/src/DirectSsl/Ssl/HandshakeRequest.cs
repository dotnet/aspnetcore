// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;

/// <summary>
/// Represents a pending handshake request.
/// </summary>
internal sealed class HandshakeRequest
{
    public Socket ClientSocket { get; }
    public IntPtr Ssl { get; set; }
    public int ClientFd { get; }
    public TaskCompletionSource<HandshakeResult> Completion { get; }
    public int WorkerId { get; set; } = -1;

    public HandshakeRequest(Socket clientSocket)
    {
        ClientSocket = clientSocket;
        ClientFd = (int)clientSocket.Handle;
        Completion = new TaskCompletionSource<HandshakeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>
/// Result of a handshake operation.
/// </summary>
public enum HandshakeResult
{
    /// <summary>
    /// Handshake succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// Handshake failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Handshake timed out.
    /// </summary>
    Timeout
}