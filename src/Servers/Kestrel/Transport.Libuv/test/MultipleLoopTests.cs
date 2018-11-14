// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class MultipleLoopTests
    {
        private readonly LibuvFunctions _uv = new LibuvFunctions();
        private readonly ILibuvTrace _logger = new LibuvTrace(new TestApplicationErrorLogger());

        [Fact]
        public void InitAndCloseServerPipe()
        {
            var loop = new UvLoopHandle(_logger);
            var pipe = new UvPipeHandle(_logger);

            loop.Init(_uv);
            pipe.Init(loop, (a, b) => { }, true);
            pipe.Bind(@"\\.\pipe\InitAndCloseServerPipe");
            pipe.Dispose();

            loop.Run();

            pipe.Dispose();
            loop.Dispose();
        }

        [Fact]
        public void ServerPipeListenForConnections()
        {
            const string pipeName = @"\\.\pipe\ServerPipeListenForConnections";

            var loop = new UvLoopHandle(_logger);
            var serverListenPipe = new UvPipeHandle(_logger);

            loop.Init(_uv);
            serverListenPipe.Init(loop, (a, b) => { }, false);
            serverListenPipe.Bind(pipeName);
            serverListenPipe.Listen(128, async (backlog, status, error, state) =>
            {
                var serverConnectionPipe = new UvPipeHandle(_logger);
                serverConnectionPipe.Init(loop, (a, b) => { }, true);

                try
                {
                    serverListenPipe.Accept(serverConnectionPipe);
                }
                catch (Exception)
                {
                    serverConnectionPipe.Dispose();
                    return;
                }

                var writeRequest = new UvWriteReq(_logger);
                writeRequest.DangerousInit(loop);

                await writeRequest.WriteAsync(
                    serverConnectionPipe,
                    new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4 }));

                writeRequest.Dispose();
                serverConnectionPipe.Dispose();
                serverListenPipe.Dispose();

            }, null);

            var worker = new Thread(() =>
            {
                var loop2 = new UvLoopHandle(_logger);
                var clientConnectionPipe = new UvPipeHandle(_logger);
                var connect = new UvConnectRequest(_logger);

                loop2.Init(_uv);
                clientConnectionPipe.Init(loop2, (a, b) => { }, true);
                connect.DangerousInit(loop2);
                connect.Connect(clientConnectionPipe, pipeName, (handle, status, error, state) =>
                {
                    var buf = loop2.Libuv.buf_init(Marshal.AllocHGlobal(8192), 8192);
                    connect.Dispose();

                    clientConnectionPipe.ReadStart(
                        (handle2, cb, state2) => buf,
                        (handle2, status2, state2) =>
                        {
                            if (status2 == TestConstants.EOF)
                            {
                                clientConnectionPipe.Dispose();
                            }
                        },
                        null);
                }, null);
                loop2.Run();
                loop2.Dispose();
            });
            worker.Start();
            loop.Run();
            loop.Dispose();
            worker.Join();
        }

        [Fact]
        public void ServerPipeDispatchConnections()
        {
            var pipeName = @"\\.\pipe\ServerPipeDispatchConnections" + Guid.NewGuid().ToString("n");

            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);

            var serverConnectionPipe = default(UvPipeHandle);
            var serverConnectionPipeAcceptedEvent = new ManualResetEvent(false);
            var serverConnectionTcpDisposedEvent = new ManualResetEvent(false);

            var serverListenPipe = new UvPipeHandle(_logger);
            serverListenPipe.Init(loop, (a, b) => { }, false);
            serverListenPipe.Bind(pipeName);
            serverListenPipe.Listen(128, (handle, status, error, state) =>
            {
                serverConnectionPipe = new UvPipeHandle(_logger);
                serverConnectionPipe.Init(loop, (a, b) => { }, true);

                try
                {
                    serverListenPipe.Accept(serverConnectionPipe);
                    serverConnectionPipeAcceptedEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    serverConnectionPipe.Dispose();
                    serverConnectionPipe = null;
                }
            }, null);

            var serverListenTcp = new UvTcpHandle(_logger);
            serverListenTcp.Init(loop, (a, b) => { });
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            serverListenTcp.Bind(endPoint);
            var port = serverListenTcp.GetSockIPEndPoint().Port;
            serverListenTcp.Listen(128, (handle, status, error, state) =>
            {
                var serverConnectionTcp = new UvTcpHandle(_logger);
                serverConnectionTcp.Init(loop, (a, b) => { });
                serverListenTcp.Accept(serverConnectionTcp);

                serverConnectionPipeAcceptedEvent.WaitOne();

                var writeRequest = new UvWriteReq(_logger);
                writeRequest.DangerousInit(loop);
                writeRequest.Write2(
                    serverConnectionPipe,
                    new ArraySegment<ArraySegment<byte>>(new ArraySegment<byte>[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4 }) }),
                    serverConnectionTcp,
                    (handle2, status2, error2, state2) =>
                    {
                        writeRequest.Dispose();
                        serverConnectionTcp.Dispose();
                        serverConnectionTcpDisposedEvent.Set();
                        serverConnectionPipe.Dispose();
                        serverListenPipe.Dispose();
                        serverListenTcp.Dispose();
                    },
                    null);
            }, null);

            var worker = new Thread(() =>
            {
                var loop2 = new UvLoopHandle(_logger);
                var clientConnectionPipe = new UvPipeHandle(_logger);
                var connect = new UvConnectRequest(_logger);

                loop2.Init(_uv);
                clientConnectionPipe.Init(loop2, (a, b) => { }, true);
                connect.DangerousInit(loop2);
                connect.Connect(clientConnectionPipe, pipeName, (handle, status, error, state) =>
                {
                    connect.Dispose();

                    var buf = loop2.Libuv.buf_init(Marshal.AllocHGlobal(64), 64);

                    serverConnectionTcpDisposedEvent.WaitOne();

                    clientConnectionPipe.ReadStart(
                        (handle2, cb, state2) => buf,
                        (handle2, status2, state2) =>
                        {
                            if (status2 == TestConstants.EOF)
                            {
                                clientConnectionPipe.Dispose();
                                return;
                            }

                            var clientConnectionTcp = new UvTcpHandle(_logger);
                            clientConnectionTcp.Init(loop2, (a, b) => { });
                            clientConnectionPipe.Accept(clientConnectionTcp);
                            var buf2 = loop2.Libuv.buf_init(Marshal.AllocHGlobal(64), 64);
                            clientConnectionTcp.ReadStart(
                                (handle3, cb, state3) => buf2,
                                (handle3, status3, state3) =>
                                {
                                    if (status3 == TestConstants.EOF)
                                    {
                                        clientConnectionTcp.Dispose();
                                    }
                                },
                                null);
                        },
                        null);
                }, null);
                loop2.Run();
                loop2.Dispose();
            });

            var worker2 = new Thread(() =>
            {
                try
                {
                    serverConnectionPipeAcceptedEvent.WaitOne();

                    var socket = TestConnection.CreateConnectedLoopbackSocket(port);
                    socket.Send(new byte[] { 6, 7, 8, 9 });
                    socket.Shutdown(SocketShutdown.Send);
                    var cb = socket.Receive(new byte[64]);
                    socket.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            worker.Start();
            worker2.Start();

            loop.Run();
            loop.Dispose();
            worker.Join();
            worker2.Join();
        }
    }
}
