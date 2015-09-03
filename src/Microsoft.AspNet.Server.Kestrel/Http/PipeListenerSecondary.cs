// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// An implementation of <see cref="ListenerSecondary"/> using UNIX sockets.
    /// </summary>
    public class PipeListenerSecondary : ListenerSecondary
    {
        public PipeListenerSecondary(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection
        /// </summary>
        protected override UvStreamHandle CreateAcceptSocket()
        {
            var acceptSocket = new UvPipeHandle();
            acceptSocket.Init(Thread.Loop, false);
            return acceptSocket;
        }
    }
}
