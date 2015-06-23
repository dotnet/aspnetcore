// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for TestConnection
    /// </summary>
    public class TestConnection : IDisposable
    {
        private Socket _socket;
        private NetworkStream _stream;
        private StreamReader _reader;

        public TestConnection()
        {
            Create();
        }

        public void Create()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(new IPEndPoint(IPAddress.Loopback, 54321));

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
            for (var index = 0; index != text.Length; ++index)
            {
                var ch = text[index];
                await writer.WriteAsync(ch);
                await writer.FlushAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(5));
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
                    Assert.True(task.Wait(1000), "timeout");
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
            var ch = new char[1];
            var count = await _reader.ReadAsync(ch, 0, 1);
            Assert.Equal(0, count);
        }
    }
}