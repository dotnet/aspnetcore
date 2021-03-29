// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
