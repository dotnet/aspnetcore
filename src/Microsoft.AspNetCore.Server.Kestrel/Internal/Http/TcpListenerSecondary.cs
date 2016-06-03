// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    /// <summary>
    /// An implementation of <see cref="ListenerSecondary"/> using TCP sockets.
    /// </summary>
    public class TcpListenerSecondary : ListenerSecondary
    {
        public TcpListenerSecondary(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection
        /// </summary>
        protected override UvStreamHandle CreateAcceptSocket()
        {
            var acceptSocket = new UvTcpHandle(Log);
            acceptSocket.Init(Thread.Loop, Thread.QueueCloseHandle);
            acceptSocket.NoDelay(ServerOptions.NoDelay);
            return acceptSocket;
        }
    }
}
