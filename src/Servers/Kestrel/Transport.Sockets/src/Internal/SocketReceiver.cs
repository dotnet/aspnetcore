// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class SocketReceiver : SocketAwaitableEventArgs
{
    public SocketReceiver(PipeScheduler ioScheduler) : base(ioScheduler)
    {
    }

    public ValueTask<SocketOperationResult> WaitForDataAsync(Socket socket)
    {
        SetBuffer(Memory<byte>.Empty);

        if (socket.ReceiveAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }

    public ValueTask<SocketOperationResult> ReceiveAsync(Socket socket, Memory<byte> buffer)
    {
        SetBuffer(buffer);

        if (socket.ReceiveAsync(this))
        {
            return new ValueTask<SocketOperationResult>(this, 0);
        }

        var bytesTransferred = BytesTransferred;
        var error = SocketError;

        return error == SocketError.Success
            ? new ValueTask<SocketOperationResult>(new SocketOperationResult(bytesTransferred))
            : new ValueTask<SocketOperationResult>(new SocketOperationResult(CreateException(error)));
    }
}
