// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Workers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;

/// <summary>
/// Represents a pending handshake request.
/// </summary>
internal sealed class HandshakeRequest
{
    public Socket ClientSocket { get; }
    public IntPtr Ssl { get; set; }
    public int ClientFd { get; }
    public TaskCompletionSource<HandshakeRequest> Completion { get; }
    public HandshakeResult Result { get; set; }
    public SslWorker? Worker { get; set; }

    public HandshakeRequest(Socket clientSocket)
    {
        ClientSocket = clientSocket;
        ClientFd = (int)clientSocket.Handle;
        Completion = new TaskCompletionSource<HandshakeRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>
/// Result of a handshake operation.
/// </summary>
internal enum HandshakeResult
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
    /// Handshake failed on connection create step.
    /// </summary>
    ConnectionCreationFailed,

    /// <summary>
    /// Handshake timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Worker pool is closed, handshake cannot be processed.
    /// </summary>
    SslWorkerPoolClosed,
}