// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for NetworkingTests
    /// </summary>
    public class NetworkingTests
    {
        private readonly Libuv _uv;
        private readonly IKestrelTrace _logger;
        public NetworkingTests()
        {
            var engine = new KestrelEngine(LibraryManager, new TestServiceContext());
            _uv = engine.Libuv;
            _logger = engine.Log;
        }

        ILibraryManager LibraryManager
        {
            get
            {
                var locator = CallContextServiceLocator.Locator;
                if (locator == null)
                {
                    return null;
                }
                var services = locator.ServiceProvider;
                if (services == null)
                {
                    return null;
                }
                return (ILibraryManager)services.GetService(typeof(ILibraryManager));
            }
        }

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
            });
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
            tcp.Init(loop);
            var address = ServerAddress.FromUrl("http://localhost:0/");
            tcp.Bind(address);
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
            tcp.Init(loop);
            var address = ServerAddress.FromUrl("http://localhost:54321/");
            tcp.Bind(address);
            tcp.Listen(10, (stream, status, error, state) =>
            {
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop);
                stream.Accept(tcp2);
                tcp2.Dispose();
                stream.Dispose();
            }, null);
            var t = Task.Run(async () =>
            {
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                await Task.Factory.FromAsync(
                    socket.BeginConnect,
                    socket.EndConnect,
                    new IPEndPoint(IPAddress.Loopback, 54321),
                    null,
                    TaskCreationOptions.None);
                socket.Dispose();
            });
            loop.Run();
            loop.Dispose();
            await t;
        }


        [Fact]
        public async Task SocketCanRead()
        {
            int bytesRead = 0;
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop);
            var address = ServerAddress.FromUrl("http://localhost:54321/");
            tcp.Bind(address);
            tcp.Listen(10, (_, status, error, state) =>
            {
                Console.WriteLine("Connected");
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop);
                tcp.Accept(tcp2);
                var data = Marshal.AllocCoTaskMem(500);
                tcp2.ReadStart(
                    (a, b, c) => _uv.buf_init(data, 500),
                    (__, nread, errCode, error2, state2) =>
                    {
                        bytesRead += nread;
                        if (nread == 0)
                        {
                            tcp2.Dispose();
                        }
                    },
                    null);
                tcp.Dispose();
            }, null);
            Console.WriteLine("Task.Run");
            var t = Task.Run(async () =>
            {
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                await Task.Factory.FromAsync(
                    socket.BeginConnect,
                    socket.EndConnect,
                    new IPEndPoint(IPAddress.Loopback, 54321),
                    null,
                    TaskCreationOptions.None);
                await Task.Factory.FromAsync(
                    socket.BeginSend,
                    socket.EndSend,
                    new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }) },
                    SocketFlags.None,
                    null,
                    TaskCreationOptions.None);
                socket.Dispose();
            });
            loop.Run();
            loop.Dispose();
            await t;
        }

        [Fact]
        public async Task SocketCanReadAndWrite()
        {
            int bytesRead = 0;
            var loop = new UvLoopHandle(_logger);
            loop.Init(_uv);
            var tcp = new UvTcpHandle(_logger);
            tcp.Init(loop);
            var address = ServerAddress.FromUrl("http://localhost:54321/");
            tcp.Bind(address);
            tcp.Listen(10, (_, status, error, state) =>
            {
                Console.WriteLine("Connected");
                var tcp2 = new UvTcpHandle(_logger);
                tcp2.Init(loop);
                tcp.Accept(tcp2);
                var data = Marshal.AllocCoTaskMem(500);
                tcp2.ReadStart(
                    (a, b, c) => tcp2.Libuv.buf_init(data, 500),
                    (__, nread, errCode, error2, state2) =>
                    {
                        bytesRead += nread;
                        if (nread == 0)
                        {
                            tcp2.Dispose();
                        }
                        else
                        {
                            for (var x = 0; x != 2; ++x)
                            {
                                var req = new UvWriteReq(new KestrelTrace(new TestKestrelTrace()));
                                req.Init(loop);
                                req.Write(
                                    tcp2,
                                    new ArraySegment<ArraySegment<byte>>(
                                        new[] { new ArraySegment<byte>(new byte[] { 65, 66, 67, 68, 69 }) }
                                        ),
                                    (_1, _2, _3, _4) => { },
                                    null);
                            }
                        }
                    },
                    null);
                tcp.Dispose();
            }, null);
            Console.WriteLine("Task.Run");
            var t = Task.Run(async () =>
            {
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                await Task.Factory.FromAsync(
                    socket.BeginConnect,
                    socket.EndConnect,
                    new IPEndPoint(IPAddress.Loopback, 54321),
                    null,
                    TaskCreationOptions.None);
                await Task.Factory.FromAsync(
                    socket.BeginSend,
                    socket.EndSend,
                    new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }) },
                    SocketFlags.None,
                    null,
                    TaskCreationOptions.None);
                socket.Shutdown(SocketShutdown.Send);
                var buffer = new ArraySegment<byte>(new byte[2048]);
                for (; ;)
                {
                    var count = await Task.Factory.FromAsync(
                        socket.BeginReceive,
                        socket.EndReceive,
                        new[] { buffer },
                        SocketFlags.None,
                        null,
                        TaskCreationOptions.None);
                    Console.WriteLine("count {0} {1}",
                        count,
                        System.Text.Encoding.ASCII.GetString(buffer.Array, 0, count));
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