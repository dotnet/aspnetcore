// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    /// <summary>
    /// Implementation of <see cref="Listener"/> that uses UNIX domain sockets as its transport.
    /// </summary>
    public class PipeListener : Listener
    {
        public PipeListener(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        protected override UvStreamHandle CreateListenSocket()
        {
            var socket = new UvPipeHandle(Log);
            socket.Init(Thread.Loop, false);
            socket.Bind(ServerAddress.UnixPipePath);
            socket.Listen(Constants.ListenBacklog, (stream, status, error, state) => ConnectionCallback(stream, status, error, state), this);
            return socket;
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        protected override void OnConnection(UvStreamHandle listenSocket, int status)
        {
            var acceptSocket = new UvPipeHandle(Log);

            try
            {
                acceptSocket.Init(Thread.Loop, false);
                listenSocket.Accept(acceptSocket);
                DispatchConnection(acceptSocket);
            }
            catch (UvException ex)
            {
                Log.LogError(0, ex, "PipeListener.OnConnection");
                acceptSocket.Dispose();
                return;
            }
        }
    }
}
