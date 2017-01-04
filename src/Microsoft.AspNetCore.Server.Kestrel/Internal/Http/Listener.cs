// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    /// <summary>
    /// Base class for listeners in Kestrel. Listens for incoming connections
    /// </summary>
    public class Listener : ListenerContext, IAsyncDisposable
    {
        private bool _closed;

        public Listener(ServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public IKestrelTrace Log => ServiceContext.Log;

        public Task StartAsync(
            ListenOptions listenOptions,
            KestrelThread thread)
        {
            ListenOptions = listenOptions;
            Thread = thread;

            var tcs = new TaskCompletionSource<int>(this);

            Thread.Post(state =>
            {
                var tcs2 = (TaskCompletionSource<int>) state;
                try
                {
                    var listener = ((Listener) tcs2.Task.AsyncState);
                    listener.ListenSocket = listener.CreateListenSocket();
                    ListenSocket.Listen(Constants.ListenBacklog, ConnectionCallback, this);
                    tcs2.SetResult(0);
                }
                catch (Exception ex)
                {
                    tcs2.SetException(ex);
                }
            }, tcs);

            return tcs.Task;
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        private UvStreamHandle CreateListenSocket()
        {
            switch (ListenOptions.Type)
            {
                case ListenType.IPEndPoint:
                case ListenType.FileHandle:
                    var socket = new UvTcpHandle(Log);

                    try
                    {
                        socket.Init(Thread.Loop, Thread.QueueCloseHandle);
                        socket.NoDelay(ListenOptions.NoDelay);

                        if (ListenOptions.Type == ListenType.IPEndPoint)
                        {
                            socket.Bind(ListenOptions.IPEndPoint);

                            // If requested port was "0", replace with assigned dynamic port.
                            ListenOptions.IPEndPoint = socket.GetSockIPEndPoint();
                        }
                        else
                        {
                            socket.Open((IntPtr)ListenOptions.FileHandle);
                        }
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }

                    return socket;
                case ListenType.SocketPath:
                    var pipe = new UvPipeHandle(Log);

                    try
                    {
                        pipe.Init(Thread.Loop, Thread.QueueCloseHandle, false);
                        pipe.Bind(ListenOptions.SocketPath);
                    }
                    catch
                    {
                        pipe.Dispose();
                        throw;
                    }

                    return pipe;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void ConnectionCallback(UvStreamHandle stream, int status, Exception error, object state)
        {
            var listener = (Listener) state;

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
            catch (UvException ex)
            {
                Log.LogError(0, ex, "Listener.OnConnection");
                acceptSocket?.Dispose();
            }
        }

        protected virtual void DispatchConnection(UvStreamHandle socket)
        {
            var connection = new Connection(this, socket);
            connection.Start();
        }

        public virtual async Task DisposeAsync()
        {
            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then KestrelEngine will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (Thread.FatalError == null && ListenSocket != null)
            {
                await Thread.PostAsync(state =>
                {
                    var listener = (Listener)state;
                    listener.ListenSocket.Dispose();

                    listener._closed = true;

                }, this).ConfigureAwait(false);
            }

            ListenSocket = null;
        }
    }
}
