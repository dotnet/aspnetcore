// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed class SocketReceiver : SocketAwaitableEventArgs
    {
        public SocketReceiver(PipeScheduler ioScheduler) : base(ioScheduler)
        {
        }

        public ValueTask<int> WaitForDataAsync(Socket socket)
        {
            SetBuffer(Memory<byte>.Empty);

            if (socket.ReceiveAsync(this))
            {
                return new ValueTask<int>(this, 0);
            }

            var bytesTransferred = BytesTransferred;
            var error = SocketError;

            return error == SocketError.Success ?
                new ValueTask<int>(bytesTransferred) :
               ValueTask.FromException<int>(CreateException(error));
        }

        public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> buffer)
        {
            SetBuffer(buffer);

            if (socket.ReceiveAsync(this))
            {
                return new ValueTask<int>(this, 0);
            }

            var bytesTransferred = BytesTransferred;
            var error = SocketError;

            return error == SocketError.Success ?
                new ValueTask<int>(bytesTransferred) :
               ValueTask.FromException<int>(CreateException(error));
        }
    }
}
