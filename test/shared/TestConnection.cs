// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Summary description for TestConnection
    /// </summary>
    public class TestConnection : IDisposable
    {
        private Socket _socket;
        private NetworkStream _stream;
        private StreamReader _reader;

        public TestConnection(int port)
        {
            Create(port);
        }

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

        public async Task SendAll(params string[] lines)
        {
            var text = string.Join("\r\n", lines);
            var writer = new StreamWriter(_stream, Encoding.GetEncoding("iso-8859-1"));
            await writer.WriteAsync(text);
            writer.Flush();
            _stream.Flush();
        }

        public async Task SendAllTryEnd(params string[] lines)
        {
            await SendAll(lines);

            try
            {
                _socket.Shutdown(SocketShutdown.Send);
            }
            catch (IOException)
            {
                // The server may forcefully close the connection (usually due to a bad request),
                // so an IOException: "An existing connection was forcibly closed by the remote host"
                // isn't guaranteed but not unexpected.
            }
        }

        public async Task Send(params string[] lines)
        {
            var text = string.Join("\r\n", lines);
            var writer = new StreamWriter(_stream, Encoding.GetEncoding("iso-8859-1"));
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
            var expected = string.Join("\r\n", lines);
            var actual = new char[expected.Length];
            var offset = 0;
            while (offset < expected.Length)
            {
                var task = _reader.ReadAsync(actual, offset, actual.Length - offset);
                if (!Debugger.IsAttached)
                {
                    Assert.True(await Task.WhenAny(task, Task.Delay(TimeSpan.FromMinutes(1))) == task, "TestConnection.Receive timed out.");
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
            var count = await _reader.ReadAsync(ch, 0, 128).TimeoutAfter(TimeSpan.FromMinutes(1));
            var text = new string(ch, 0, count);
            Assert.Equal("", text);
        }

        public async Task ReceiveForcedEnd(params string[] lines)
        {
            await Receive(lines);

            try
            {
                var ch = new char[128];
                var count = await _reader.ReadAsync(ch, 0, 128).TimeoutAfter(TimeSpan.FromMinutes(1));
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

        public Task WaitForConnectionClose()
        {
            var tcs = new TaskCompletionSource<object>();
            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.SetBuffer(new byte[1], 0, 1);
            eventArgs.Completed += ReceiveAsyncCompleted;
            eventArgs.UserToken = tcs;

            if (!_socket.ReceiveAsync(eventArgs))
            {
                ReceiveAsyncCompleted(this, eventArgs);
            }

            return tcs.Task;
        }

        private void ReceiveAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0)
            {
                var tcs = (TaskCompletionSource<object>)e.UserToken;
                tcs.SetResult(null);
            }
        }

        public static Socket CreateConnectedLoopbackSocket(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            return socket;
        }
    }
}
