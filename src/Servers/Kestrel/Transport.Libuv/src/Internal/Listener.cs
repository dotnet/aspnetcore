// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    /// <summary>
    /// Base class for listeners in Kestrel. Listens for incoming connections
    /// </summary>
    internal class Listener : ListenerContext, IAsyncDisposable
    {
        // REVIEW: This needs to be bounded and we need a strategy for what to do when the queue is full
        private bool _closed;

        public Listener(LibuvTransportContext transportContext) : base(transportContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public ILibuvTrace Log => TransportContext.Log;

        public Task StartAsync(
            EndPoint endPoint,
            LibuvThread thread)
        {
            EndPoint = endPoint;
            Thread = thread;

            return Thread.PostAsync(listener =>
            {
                listener.ListenSocket = listener.CreateListenSocket();
                listener.ListenSocket.Listen(TransportContext.Options.Backlog, ConnectionCallback, listener);
            }, this);
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        private UvStreamHandle CreateListenSocket()
        {
            switch (EndPoint)
            {
                case IPEndPoint _:
                    return ListenTcp(useFileHandle: false);
                case UnixDomainSocketEndPoint _:
                    return ListenPipe(useFileHandle: false);
                case FileHandleEndPoint _:
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
                socket.NoDelay(TransportContext.Options.NoDelay);

                if (!useFileHandle)
                {
                    socket.Bind((IPEndPoint)EndPoint);

                    // If requested port was "0", replace with assigned dynamic port.
                    EndPoint = socket.GetSockIPEndPoint();
                }
                else
                {
                    socket.Open((IntPtr)((FileHandleEndPoint)EndPoint).FileHandle);
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
                    // UnixDomainSocketEndPoint.ToString() returns the path
                    pipe.Bind(EndPoint.ToString());
                }
                else
                {
                    pipe.Open((IntPtr)((FileHandleEndPoint)EndPoint).FileHandle);
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
            var handleEndPoint = (FileHandleEndPoint)EndPoint;

            switch (handleEndPoint.FileHandleType)
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
                EndPoint = new FileHandleEndPoint(handleEndPoint.FileHandle, FileHandleType.Tcp);
                return handle;
            }
            catch (UvException exception) when (exception.StatusCode == LibuvConstants.ENOTSUP)
            {
                Log.LogDebug(0, exception, "Listener.ListenHandle");
            }

            handle = ListenPipe(useFileHandle: true);
            EndPoint = new FileHandleEndPoint(handleEndPoint.FileHandle, FileHandleType.Pipe);
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
            HandleConnection(socket);
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

                    listener.StopAcceptingConnections();

                }, this).ConfigureAwait(false);
            }

            ListenSocket = null;
        }
    }
}
