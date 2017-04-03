// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class ListenerContext
    {
        public ListenerContext(LibuvTransportContext transportContext)
        {
            TransportContext = transportContext;
        }

        public LibuvTransportContext TransportContext { get; set; }

        public IEndPointInformation EndPointInformation { get; set; }

        public KestrelThread Thread { get; set; }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection.
        /// </summary>
        protected UvStreamHandle CreateAcceptSocket()
        {
            switch (EndPointInformation.Type)
            {
                case ListenType.IPEndPoint:
                case ListenType.FileHandle:
                    var tcpHandle = new UvTcpHandle(TransportContext.Log);
                    tcpHandle.Init(Thread.Loop, Thread.QueueCloseHandle);
                    tcpHandle.NoDelay(EndPointInformation.NoDelay);
                    return tcpHandle;
                case ListenType.SocketPath:
                    var pipeHandle = new UvPipeHandle(TransportContext.Log);
                    pipeHandle.Init(Thread.Loop, Thread.QueueCloseHandle);
                    return pipeHandle;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
