// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// A secondary listener is delegated requests from a primary listener via a named pipe or 
    /// UNIX domain socket.
    /// </summary>
    public abstract class ListenerSecondary : ListenerContext, IDisposable
    {
        private string _pipeName;
        private IntPtr _ptr;
        private Libuv.uv_buf_t _buf;

        protected ListenerSecondary(ServiceContext serviceContext) : base(serviceContext)
        {
            _ptr = Marshal.AllocHGlobal(4);
            _buf = Thread.Loop.Libuv.buf_init(_ptr, 4);
        }

        UvPipeHandle DispatchPipe { get; set; }

        public Task StartAsync(
            string pipeName,
            ServerAddress address,
            KestrelThread thread,
            RequestDelegate application)
        {
            _pipeName = pipeName;
            ServerAddress = address;
            Thread = thread;
            Application = application;

            DispatchPipe = new UvPipeHandle(Log);

            var tcs = new TaskCompletionSource<int>(this);
            Thread.Post(tcs2 => StartCallback(tcs2), tcs);
            return tcs.Task;
        }

        private static void StartCallback(TaskCompletionSource<int> tcs)
        {
            var listener = (ListenerSecondary)tcs.Task.AsyncState;
            listener.StartedCallback(tcs);
        }

        private void StartedCallback(TaskCompletionSource<int> tcs)
        {
            try
            {
                DispatchPipe.Init(Thread.Loop, true);
                var connect = new UvConnectRequest(Log);
                connect.Init(Thread.Loop);
                connect.Connect(
                    DispatchPipe,
                    _pipeName,
                    (connect2, status, error, state) => ConnectCallback(connect2, status, error, (TaskCompletionSource<int>)state),
                    tcs);
            }
            catch (Exception ex)
            {
                DispatchPipe.Dispose();
                tcs.SetException(ex);
            }
        }

        private static void ConnectCallback(UvConnectRequest connect, int status, Exception error, TaskCompletionSource<int> tcs)
        {
            var listener = (ListenerSecondary)tcs.Task.AsyncState;
            listener.ConnectedCallback(connect, status, error, tcs);
        }

        private void ConnectedCallback(UvConnectRequest connect, int status, Exception error, TaskCompletionSource<int> tcs)
        {
            connect.Dispose();
            if (error != null)
            {
                tcs.SetException(error);
                return;
            }

            try
            {
                DispatchPipe.ReadStart(
                    (handle, status2, state) => ((ListenerSecondary)state)._buf,
                    (handle, status2, state) => 
                        {
                            var listener = ((ListenerSecondary)state);
                            listener.ReadStartCallback(handle, status2, listener._ptr);
                        }, this);

                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                DispatchPipe.Dispose();
                tcs.SetException(ex);
            }
        }

        private void ReadStartCallback(UvStreamHandle handle, int status, IntPtr ptr)
        {
            if (status < 0)
            {
                if (status != Constants.EOF)
                {
                    Exception ex;
                    Thread.Loop.Libuv.Check(status, out ex);
                    Log.LogError("DispatchPipe.ReadStart", ex);
                }

                DispatchPipe.Dispose();
                Marshal.FreeHGlobal(ptr);
                return;
            }

            if (DispatchPipe.PendingCount() == 0)
            {
                return;
            }

            var acceptSocket = CreateAcceptSocket();

            try
            {
                DispatchPipe.Accept(acceptSocket);
            }
            catch (UvException ex)
            {
                Log.LogError("DispatchPipe.Accept", ex);
                acceptSocket.Dispose();
                return;
            }

            var connection = new Connection(this, acceptSocket);
            connection.Start();
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection
        /// </summary>
        protected abstract UvStreamHandle CreateAcceptSocket();

        public void Dispose()
        {
            Marshal.FreeHGlobal(_ptr);

            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then KestrelEngine will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (Thread.FatalError == null)
            {
                Thread.Send(listener => ((ListenerSecondary)listener).DispatchPipe.Dispose(), this);
            }
        }
    }
}
