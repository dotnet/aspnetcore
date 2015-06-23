// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// Summary description for Accept
    /// </summary>
    public class Listener : ListenerContext, IDisposable
    {
        private static readonly Action<UvStreamHandle, int, Exception, object> _connectionCallback = ConnectionCallback;

        UvTcpHandle ListenSocket { get; set; }

        private static void ConnectionCallback(UvStreamHandle stream, int status, Exception error, object state)
        {
            if (error != null)
            {
                Trace.WriteLine("Listener.ConnectionCallback " + error.ToString());
            }
            else
            {
                ((Listener)state).OnConnection(stream, status);
            }
        }

        public Listener(IMemoryPool memory)
        {
            Memory = memory;
        }

        public Task StartAsync(
            string scheme,
            string host,
            int port,
            KestrelThread thread,
            Func<Frame, Task> application)
        {
            Thread = thread;
            Application = application;

            var tcs = new TaskCompletionSource<int>();
            Thread.Post(_ =>
            {
                try
                {
                    ListenSocket = new UvTcpHandle();
                    ListenSocket.Init(Thread.Loop, Thread.QueueCloseHandle);
                    ListenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                    ListenSocket.Listen(10, _connectionCallback, this);
                    tcs.SetResult(0);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            return tcs.Task;
        }

        private void OnConnection(UvStreamHandle listenSocket, int status)
        {
            var acceptSocket = new UvTcpHandle();
            acceptSocket.Init(Thread.Loop, Thread.QueueCloseHandle);
            listenSocket.Accept(acceptSocket);

            var connection = new Connection(this, acceptSocket);
            connection.Start();
        }

        public void Dispose()
        {
            var tcs = new TaskCompletionSource<int>();
            Thread.Post(
                _ =>
                {
                    try
                    {
                        ListenSocket.Dispose();
                        tcs.SetResult(0);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                },
                null);
            tcs.Task.Wait();
            ListenSocket = null;
        }
    }
}
