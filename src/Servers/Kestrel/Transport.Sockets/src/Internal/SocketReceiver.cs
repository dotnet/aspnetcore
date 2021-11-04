// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed class SocketReceiver : SocketAwaitableEventArgs
    {
        public SocketReceiver(PipeScheduler ioScheduler) : base(ioScheduler)
        {
        }

        public ValueTask<TransferResult> WaitForDataAsync(Socket socket)
        {
            SetBuffer(Memory<byte>.Empty);

            if (socket.ReceiveAsync(this))
            {
                return new ValueTask<TransferResult>(this, 0);
            }

            var bytesTransferred = BytesTransferred;
            var error = SocketError;

            return error == SocketError.Success 
                ? new ValueTask<TransferResult>(new TransferResult(bytesTransferred)) 
                : new ValueTask<TransferResult>(new TransferResult(CreateException(error)))
                ;
        }

        public ValueTask<TransferResult> ReceiveAsync(Socket socket, Memory<byte> buffer)
        {
            SetBuffer(buffer);

            if (socket.ReceiveAsync(this))
            {
                return new ValueTask<TransferResult>(this, 0);
            }

            var bytesTransferred = BytesTransferred;
            var error = SocketError;

            return error == SocketError.Success 
                ? new ValueTask<TransferResult>(new TransferResult(bytesTransferred)) 
                : new ValueTask<TransferResult>(new TransferResult(CreateException(error)))
                ;
        }
    }
}
