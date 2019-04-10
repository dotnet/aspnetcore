// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    /// <summary>
    /// Base class for listeners in Kestrel. Listens for incoming connections
    /// </summary>
    public class Listener : ListenerContext, IAsyncDisposable
    {
        private bool _closed;

        public Listener(LibuvTransportContext transportContext) : base(transportContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public ILibuvTrace Log => TransportContext.Log;

        public Task StartAsync(
            IEndPointInformation endPointInformation,
            LibuvThread thread)
        {
            EndPointInformation = endPointInformation;
            Thread = thread;

            return Thread.PostAsync(listener =>
            {
                listener.ListenSocket = listener.CreateListenSocket();
                listener.ListenSocket.Listen(LibuvConstants.ListenBacklog, ConnectionCallback, listener);
            }, this);
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        private UvStreamHandle CreateListenSocket()
        {
            switch (EndPointInformation.Type)
            {
                case ListenType.IPEndPoint:
                    return ListenTcp(useFileHandle: false);
                case ListenType.SocketPath:
                    return ListenPipe(useFileHandle: false);
                case ListenType.FileHandle:
                    return ListenHandle();
                default:
                    throw new NotSupportedException();
            }
        }

        private UvTcpHandle ListenTcp(bool useFileHandle)
        {
            var socket = new UvTcpHandle(Log);

            try
            {
                socket.Init(Thread.Loop, Thread.QueueCloseHandle);
                socket.NoDelay(EndPointInformation.NoDelay);

                if (!useFileHandle)
                {
                    socket.Bind(EndPointInformation.IPEndPoint);

                    // If requested port was "0", replace with assigned dynamic port.
                    EndPointInformation.IPEndPoint = socket.GetSockIPEndPoint();
                }
                else
                {
                    socket.Open((IntPtr)EndPointInformation.FileHandle);
                }
            }
            catch
            {
                socket.Dispose();
                throw;
            }

            return socket;
        }

        private UvPipeHandle ListenPipe(bool useFileHandle)
        {
            var pipe = new UvPipeHandle(Log);

            try
            {
                pipe.Init(Thread.Loop, Thread.QueueCloseHandle, false);

                if (!useFileHandle)
                {
                    pipe.Bind(EndPointInformation.SocketPath);
                }
                else
                {
                    pipe.Open((IntPtr)EndPointInformation.FileHandle);
                }
            }
            catch
            {
                pipe.Dispose();
                throw;
            }

            return pipe;
        }

        private UvStreamHandle ListenHandle()
        {
            switch (EndPointInformation.HandleType)
            {
                case FileHandleType.Auto:
                    break;
                case FileHandleType.Tcp:
                    return ListenTcp(useFileHandle: true);
                case FileHandleType.Pipe:
                    return ListenPipe(useFileHandle: true);
                default:
                    throw new NotSupportedException();
            }

            UvStreamHandle handle;
            try
            {
                handle = ListenTcp(useFileHandle: true);
                EndPointInformation.HandleType = FileHandleType.Tcp;
                return handle;
            }
            catch (UvException exception) when (exception.StatusCode == LibuvConstants.ENOTSUP)
            {
                Log.LogDebug(0, exception, "Listener.ListenHandle");
            }

            handle = ListenPipe(useFileHandle: true);
            EndPointInformation.HandleType = FileHandleType.Pipe;
            return handle;
        }

        private static void ConnectionCallback(UvStreamHandle stream, int status, UvException error, object state)
        {
            var listener = (Listener)state;

            if (error != null)
            {
                listener.Log.LogError(0, error, "Listener.ConnectionCallback");
            }
            else if (!listener._closed)
            {
                listener.OnConnection(stream, status);
            }
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        private void OnConnection(UvStreamHandle listenSocket, int status)
        {
            UvStreamHandle acceptSocket = null;

            try
            {
                acceptSocket = CreateAcceptSocket();
                listenSocket.Accept(acceptSocket);
                DispatchConnection(acceptSocket);
            }
            catch (UvException ex) when (LibuvConstants.IsConnectionReset(ex.StatusCode))
            {
                Log.ConnectionReset("(null)");
                acceptSocket?.Dispose();
            }
            catch (UvException ex)
            {
                Log.LogError(0, ex, "Listener.OnConnection");
                acceptSocket?.Dispose();
            }
        }

        protected virtual void DispatchConnection(UvStreamHandle socket)
        {
            // REVIEW: This task should be tracked by the server for graceful shutdown
            // Today it's handled specifically for http but not for arbitrary middleware
            _ = HandleConnectionAsync(socket);
        }

        public virtual async Task DisposeAsync()
        {
            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then LibuvTransport will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (Thread.FatalError == null && ListenSocket != null)
            {
                await Thread.PostAsync(listener =>
                {
                    listener.ListenSocket.Dispose();

                    listener._closed = true;

                }, this).ConfigureAwait(false);
            }

            ListenSocket = null;
        }
    }
}
