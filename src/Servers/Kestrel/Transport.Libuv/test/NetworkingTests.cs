// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    /// <summary>
    /// Summary description for NetworkingTests
    /// </summary>
    public class NetworkingTests
    {
        private readonly LibuvFunctions _uv = new LibuvFunctions();
        private readonly ILibuvTrace _logger = new LibuvTrace(new TestApplicationErrorLogger());

        [Fact]
        public void LoopCanBeInitAndClose()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            loop.Run();
            loop.Dispose();
        }

        [Fact]
        public void AsyncCanBeSent()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var trigger = new UvAsyncHandle(_logger);
            var called = false;
            trigger.Init(loop, () =>
            {
                called = true;
                trigger.Dispose();
            }, (a, b) => { });
            trigger.Send();
            loop.Run();
            loop.Dispose();
            Assert.True(called);
        }

        [Fact]
        public void SocketCanBeInitAndClose()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop, (a, b) => { });
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            tcp.Bind(endPoint);
            tcp.Dispose();
            loop.Run();
            loop.Dispose();
        }

        [Fact]
        public async Task SocketCanListenAndAccept()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop, (a, b) => { });
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            tcp.Bind(endPoint);
            var port = tcp.GetSockIPEndPoint().Port;
            tcp.Listen(10, (stream, status, error, state) =>
            {
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop, (a, b) => { });
                stream.Accept(tcp2);
                tcp2.Dispose();
                stream.Dispose();
            }, null);
            var t = Task.Run(() =>
            {
                var socket = TestConnection.CreateConnectedLoopbackSocket(port);
                socket.Dispose();
            });
            loop.Run();
            loop.Dispose();
            await t;
        }

        [Fact]
        public async Task SocketCanRead()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop, (a, b) => { });
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            tcp.Bind(endPoint);
            var port = tcp.GetSockIPEndPoint().Port;
            tcp.Listen(10, (_, status, error, state) =>
            {
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop, (a, b) => { });
                tcp.Accept(tcp2);
                var data = Marshal.AllocCoTaskMem(500);
                tcp2.ReadStart(
                    (a, b, c) => _uv.buf_init(data, 500),
                    (__, nread, state2) =>
                    {
                        if (nread <= 0)
                        {
                            tcp2.Dispose();
                        }
                    },
                    null);
                tcp.Dispose();
            }, null);
            var t = Task.Run(async () =>
            {
                var socket = TestConnection.CreateConnectedLoopbackSocket(port);
                await socket.SendAsync(new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }) },
                                       SocketFlags.None);
                socket.Dispose();
            });
            loop.Run();
            loop.Dispose();
            await t;
        }

        [Fact]
        public async Task SocketCanReadAndWrite()
        {
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop, (a, b) => { });
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            tcp.Bind(endPoint);
            var port = tcp.GetSockIPEndPoint().Port;
            tcp.Listen(10, (_, status, error, state) =>
            {
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop, (a, b) => { });
                tcp.Accept(tcp2);
                var data = Marshal.AllocCoTaskMem(500);
                tcp2.ReadStart(
                    (a, b, c) => tcp2.Libuv.buf_init(data, 500),
                    async (__, nread, state2) =>
                    {
                        if (nread <= 0)
                        {
                            tcp2.Dispose();
                        }
                        else
                        {
                            for (var x = 0; x < 2; x++)
                            {
                                var req = new UvWriteReq(_logger);
                                req.DangerousInit(loop);
                                var block = new ReadOnlySequence<byte>(new byte[] { 65, 66, 67, 68, 69 });

                                await req.WriteAsync(
                                    tcp2,
                                    block);
                            }
                        }
                    },
                    null);
                tcp.Dispose();
            }, null);
            var t = Task.Run(async () =>
            {
                var socket = TestConnection.CreateConnectedLoopbackSocket(port);
                await socket.SendAsync(new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }) },
                                       SocketFlags.None);
                socket.Shutdown(SocketShutdown.Send);
                var buffer = new ArraySegment<byte>(new byte[2048]);
                while (true)
                {
                    var count = await socket.ReceiveAsync(new[] { buffer }, SocketFlags.None);
                    if (count <= 0) break;
                }
                socket.Dispose();
            });
            loop.Run();
            loop.Dispose();
            await t;
        }
    }
}