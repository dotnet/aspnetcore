// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestralTests
{
    /// <summary>
    /// Summary description for NetworkingTests
    /// </summary>
    public class NetworkingTests
    {
        Libuv _uv;
        public NetworkingTests()
        {
            _uv = new Libuv();
            _uv.Load("libuv.dll");
        }

        [Fact]
        public async Task LoopCanBeInitAndClose()
        {
            var loop = new UvLoopHandle();
            loop.Init(_uv);
            loop.Run();
            loop.Close();
        }

        [Fact]
        public async Task AsyncCanBeSent()
        {
            var loop = new UvLoopHandle();
            loop.Init(_uv);
            var trigger = new UvAsyncHandle();
            var called = false;
            trigger.Init(loop, () =>
            {
                called = true;
                trigger.Close();
            });
            trigger.Send();
            loop.Run();
            loop.Close();
            Assert.True(called);
        }

        [Fact]
        public async Task SocketCanBeInitAndClose()
        {
            var loop = new UvLoopHandle();
            loop.Init(_uv);
            var tcp = new UvTcpHandle();
            tcp.Init(loop);
            tcp.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            tcp.Close();
            loop.Run();
            loop.Close();
        }


        [Fact]
        public async Task SocketCanListenAndAccept()
        {
            var loop = new UvLoopHandle();
            loop.Init(_uv);
            var tcp = new UvTcpHandle();
            tcp.Init(loop);
            tcp.Bind(new IPEndPoint(IPAddress.Loopback, 54321));
            tcp.Listen(10, (stream, status, state) =>
            {
                var tcp2 = new UvTcpHandle();
                tcp2.Init(loop);
                stream.Accept(tcp2);
                tcp2.Close();
                stream.Close();
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
                socket.Close();
            });
            loop.Run();
            loop.Close();
            await t;
        }


        [Fact]
        public async Task SocketCanRead()
        {
            int bytesRead = 0;
            var loop = new UvLoopHandle();
            loop.Init(_uv);
            var tcp = new UvTcpHandle();
            tcp.Init(loop);
            tcp.Bind(new IPEndPoint(IPAddress.Loopback, 54321));
            tcp.Listen(10, (_, status, state) =>
            {
                var tcp2 = new UvTcpHandle();
                tcp2.Init(loop);
                tcp.Accept(tcp2);
                tcp2.ReadStart((__, nread, data, state2) =>
                {
                    bytesRead += nread;
                    if (nread == 0)
                    {
                        tcp2.Close();
                    }
                }, null);
                tcp.Close();
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
                await Task.Factory.FromAsync(
                    socket.BeginSend,
                    socket.EndSend,
                    new[] { new ArraySegment<byte>(new byte[] { 1, 2, 3, 4, 5 }) },
                    SocketFlags.None,
                    null,
                    TaskCreationOptions.None);
                socket.Close();
            });
            loop.Run();
            loop.Close();
            await t;
        }
    }
}