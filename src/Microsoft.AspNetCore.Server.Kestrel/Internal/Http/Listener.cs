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
    public abstract class Listener : ListenerContext, IAsyncDisposable
    {
        private bool _closed;

        protected Listener(ServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public IKestrelTrace Log => ServiceContext.Log;

        public Task StartAsync(
            ServerAddress address,
            KestrelThread thread)
        {
            ServerAddress = address;
            Thread = thread;

            var tcs = new TaskCompletionSource<int>(this);

            Thread.Post(state =>
            {
                var tcs2 = (TaskCompletionSource<int>)state;
                try
                {
                    var listener = ((Listener)tcs2.Task.AsyncState);
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
        protected abstract UvStreamHandle CreateListenSocket();

        private static void ConnectionCallback(UvStreamHandle stream, int status, Exception error, object state)
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
        protected abstract void OnConnection(UvStreamHandle listenSocket, int status);

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
