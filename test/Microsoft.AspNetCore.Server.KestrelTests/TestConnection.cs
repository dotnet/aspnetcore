// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for TestConnection
    /// </summary>
    public class TestConnection : IDisposable
    {
        private Socket _socket;
        private NetworkStream _stream;
        private StreamReader _reader;

        public TestConnection(TestServer server)
        {
            Server = server;
            Create(server.Port);
        }

        public TestServer Server { get; }

        public void Create(int port)
        {
            _socket = CreateConnectedLoopbackSocket(port);

            _stream = new NetworkStream(_socket, false);
            _reader = new StreamReader(_stream, Encoding.ASCII);
        }
        public void Dispose()
        {
            _stream.Dispose();
            _socket.Dispose();
        }

        public async Task Send(params string[] lines)
        {
            var text = String.Join("\r\n", lines);
            var writer = new StreamWriter(_stream, Encoding.ASCII);
            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                await writer.WriteAsync(ch);
                await writer.FlushAsync();
                // Re-add delay to help find socket input consumption bugs more consistently
                //await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
            writer.Flush();
            _stream.Flush();
        }

        public async Task SendEnd(params string[] lines)
        {
            await Send(lines);
            _socket.Shutdown(SocketShutdown.Send);
        }

        public async Task Receive(params string[] lines)
        {
            var expected = String.Join("\r\n", lines);
            var actual = new char[expected.Length];
            var offset = 0;
            while (offset < expected.Length)
            {
                var task = _reader.ReadAsync(actual, offset, actual.Length - offset);
                if (!Debugger.IsAttached)
                {
                    Assert.True(task.Wait(4000), "timeout");
                }
                var count = await task;
                if (count == 0)
                {
                    break;
                }
                offset += count;
            }

            Assert.Equal(expected, new String(actual, 0, offset));
        }

        public async Task ReceiveEnd(params string[] lines)
        {
            await Receive(lines);
            var ch = new char[128];
            var count = await _reader.ReadAsync(ch, 0, 128);
            var text = new string(ch, 0, count);
            Assert.Equal("", text);
        }

        public async Task ReceiveForcedEnd(params string[] lines)
        {
            await Receive(lines);

            try
            {
                var ch = new char[128];
                var count = await _reader.ReadAsync(ch, 0, 128);
                var text = new string(ch, 0, count);
                Assert.Equal("", text);
            }
            catch (IOException)
            {
                // The server is forcefully closing the connection so an IOException:
                // "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."
                // isn't guaranteed but not unexpected.
            }
        }

        public static Socket CreateConnectedLoopbackSocket(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (PlatformApis.IsWindows)
            {
                const int SIO_LOOPBACK_FAST_PATH = -1744830448;
                var optionInValue = BitConverter.GetBytes(1);
                try
                {
                    socket.IOControl(SIO_LOOPBACK_FAST_PATH, optionInValue, null);
                }
                catch
                {
                    // If the operating system version on this machine did
                    // not support SIO_LOOPBACK_FAST_PATH (i.e. version
                    // prior to Windows 8 / Windows Server 2012), handle the exception
                }
            }
            socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            return socket;
        }
    }
}
