// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class ListenerContext
    {
        public ListenerContext() { }

        public ListenerContext(ListenerContext context)
        {
            Thread = context.Thread;
            Application = context.Application;
            Memory = context.Memory;
        }

        public KestrelThread Thread { get; set; }

        public Func<object, Task> Application { get; set; }

        public IMemoryPool Memory { get; set; }
    }

    /// <summary>
    /// Summary description for Accept
    /// </summary>
    public class Listener : ListenerContext, IDisposable
    {
        private static readonly Action<UvStreamHandle, int, object> _connectionCallback = ConnectionCallback;

        UvTcpHandle ListenSocket { get; set; }

        private static void ConnectionCallback(UvStreamHandle stream, int status, object state)
        {
            ((Listener)state).OnConnection(stream, status);
        }

        public Listener(IMemoryPool memory)
        {
            Memory = memory;
        }

        public Task StartAsync(KestrelThread thread, Func<object, Task> app)
        {
            Thread = thread;
            Application = app;

            var tcs = new TaskCompletionSource<int>();
            Thread.Post(OnStart, tcs);
            return tcs.Task;
        }

        public void OnStart(object parameter)
        {
            var tcs = (TaskCompletionSource<int>)parameter;
            try
            {
                ListenSocket = new UvTcpHandle();
                ListenSocket.Init(Thread.Loop);
                ListenSocket.Bind(new IPEndPoint(IPAddress.Any, 4001));
                ListenSocket.Listen(10, _connectionCallback, this);
                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        private void OnConnection(UvStreamHandle listenSocket, int status)
        {
            var acceptSocket = new UvTcpHandle();
            acceptSocket.Init(Thread.Loop);
            listenSocket.Accept(acceptSocket);

            var connection = new Connection(this, acceptSocket);
            connection.Start();
        }

        public void Dispose()
        {
            Thread.Post(OnDispose, ListenSocket);
            ListenSocket = null;
        }

        private void OnDispose(object listenSocket)
        {
            ((UvHandle)listenSocket).Close();
        }
    }
}
