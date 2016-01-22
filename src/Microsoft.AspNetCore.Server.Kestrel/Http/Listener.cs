// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Base class for listeners in Kestrel. Listens for incoming connections
    /// </summary>
    public abstract class Listener : ListenerContext, IDisposable
    {
        protected Listener(ServiceContext serviceContext) 
            : base(serviceContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public Task StartAsync(
            ServerAddress address,
            KestrelThread thread)
        {
            ServerAddress = address;
            Thread = thread;

            var tcs = new TaskCompletionSource<int>(this);

            Thread.Post(tcs2 =>
            {
                try
                {
                    var listener = ((Listener)tcs2.Task.AsyncState);
                    listener.ListenSocket = listener.CreateListenSocket();
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
        protected abstract UvStreamHandle CreateListenSocket();

        protected static void ConnectionCallback(UvStreamHandle stream, int status, Exception error, object state)
        {
            var listener = (Listener) state;
            if (error != null)
            {
                listener.Log.LogError("Listener.ConnectionCallback ", error);
            }
            else
            {
                listener.OnConnection(stream, status);
            }
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        protected abstract void OnConnection(UvStreamHandle listenSocket, int status);

        protected virtual void DispatchConnection(UvStreamHandle socket)
        {
            var connection = new Connection(this, socket);
            connection.Start();
        }

        public void Dispose()
        {
            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then KestrelEngine will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (Thread.FatalError == null && ListenSocket != null)
            {
                var tcs = new TaskCompletionSource<int>(this);
                Thread.Post(
                    tcs2 =>
                    {
                        try
                        {
                            var socket = (Listener)tcs2.Task.AsyncState;
                            socket.ListenSocket.Dispose();

                            var writeReqPool = socket.WriteReqPool;
                            while (writeReqPool.Count > 0)
                            {
                                writeReqPool.Dequeue().Dispose();
                            }

                            tcs2.SetResult(0);
                        }
                        catch (Exception ex)
                        {
                            tcs2.SetException(ex);
                        }
                    },
                    tcs);

                // REVIEW: Should we add a timeout here to be safe?
                tcs.Task.Wait();
            }

            ListenSocket = null;
        }
    }
}
