// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed class SocketReceiver : SocketAwaitableEventArgs
    {
        public SocketReceiver(PipeScheduler ioScheduler) : base(ioScheduler)
        {
        }

        public SocketAwaitableEventArgs WaitForDataAsync(Socket socket)
        {
            SetBuffer(Memory<byte>.Empty);

            if (!socket.ReceiveAsync(this))
            {
                Complete();
            }

            return this;
        }

        public SocketAwaitableEventArgs ReceiveAsync(Socket socket, Memory<byte> buffer)
        {
            SetBuffer(buffer);

            if (!socket.ReceiveAsync(this))
            {
                Complete();
            }

            return this;
        }
    }
}
