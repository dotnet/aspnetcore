// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

internal sealed class SocketAccepter : SocketAwaitableEventArgs
{
    public SocketAccepter(PipeScheduler ioScheduler) : base(ioScheduler)
    {
    }

    public ValueTask<SocketOperationResult> AcceptAsync(Socket socket) //, Memory<byte> buffer = default)
    {
        AcceptSocket = null; // we don't currently allow reuse
        //SetBuffer(buffer);

        if (socket.AcceptAsync(this))
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
