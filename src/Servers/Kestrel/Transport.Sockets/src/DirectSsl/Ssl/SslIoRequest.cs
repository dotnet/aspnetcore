// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Ssl;

/// <summary>
/// Type of SSL I/O operation.
/// </summary>
internal enum SslIoType
{
    Read,
    Write
}

/// <summary>
/// Represents a pending SSL read or write operation.
/// Used by SslWorker to handle I/O in its event loop.
/// </summary>
internal sealed class SslIoRequest
{
    public SslIoType Type { get; }
    public IntPtr Ssl { get; }
    public int ClientFd { get; }
    
    /// <summary>
    /// Buffer for read operations (mutable - we write into it).
    /// </summary>
    public Memory<byte> ReadBuffer { get; }
    
    /// <summary>
    /// Buffer for write operations (read-only - we read from it).
    /// </summary>
    public ReadOnlyMemory<byte> WriteBuffer { get; }
    
    public int Length { get; }
    public TaskCompletionSource<int> Completion { get; }

    /// <summary>
    /// For writes: tracks how many bytes have been written so far (partial writes).
    /// </summary>
    public int BytesTransferred { get; set; }

    /// <summary>
    /// Constructor for READ operations.
    /// </summary>
    public SslIoRequest(SslIoType type, IntPtr ssl, int clientFd, Memory<byte> buffer, int length)
    {
        Type = type;
        Ssl = ssl;
        ClientFd = clientFd;
        ReadBuffer = buffer;
        Length = length;
        Completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    
    /// <summary>
    /// Constructor for WRITE operations.
    /// </summary>
    public SslIoRequest(SslIoType type, IntPtr ssl, int clientFd, ReadOnlyMemory<byte> buffer, int length)
    {
        Type = type;
        Ssl = ssl;
        ClientFd = clientFd;
        WriteBuffer = buffer;
        Length = length;
        Completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
