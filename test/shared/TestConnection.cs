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
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

        public TestConnection(int port)
            : this(port, AddressFamily.InterNetwork)
        {
        }

        public TestConnection(int port, AddressFamily addressFamily)
        {
            Create(port, addressFamily);
        }

        public StreamReader Reader => _reader;

        public void Create(int port, AddressFamily addressFamily)
        {
            _socket = CreateConnectedLoopbackSocket(port, addressFamily);

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
            await writer.WriteAsync(text).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
        }

        public async Task Send(params string[] lines)
        {
            var text = string.Join("\r\n", lines);
            var writer = new StreamWriter(_stream, Encoding.GetEncoding("iso-8859-1"));
            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];
                writer.Write(ch);
                await writer.FlushAsync().ConfigureAwait(false);
                // Re-add delay to help find socket input consumption bugs more consistently
                //await Task.Delay(TimeSpan.FromMilliseconds(5));
            }
            await writer.FlushAsync().ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
        }

        public async Task Receive(params string[] lines)
        {
            var expected = string.Join("\r\n", lines);
            var actual = new char[expected.Length];
            var offset = 0;
            while (offset < expected.Length)
            {
                var data = new byte[expected.Length];
                var task = _reader.ReadAsync(actual, offset, actual.Length - offset);
                if (!Debugger.IsAttached)
                {
                    task = task.TimeoutAfter(Timeout);
                }
                var count = await task.ConfigureAwait(false);
                if (count == 0)
                {
                    break;
                }
                offset += count;
            }

            Assert.Equal(expected, new string(actual, 0, offset));
        }

        public async Task ReceiveEnd(params string[] lines)
        {
            await Receive(lines).ConfigureAwait(false);
            _socket.Shutdown(SocketShutdown.Send);
            var ch = new char[128];
            var count = await _reader.ReadAsync(ch, 0, 128).TimeoutAfter(Timeout).ConfigureAwait(false);
            var text = new string(ch, 0, count);
            Assert.Equal("", text);
        }

        public async Task ReceiveForcedEnd(params string[] lines)
        {
            await Receive(lines).ConfigureAwait(false);

            try
            {
                var ch = new char[128];
                var count = await _reader.ReadAsync(ch, 0, 128).TimeoutAfter(Timeout).ConfigureAwait(false);
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

        public async Task ReceiveStartsWith(string prefix, int maxLineLength = 1024)
        {
            var actual = new char[maxLineLength];
            var offset = 0;

            while (offset < maxLineLength)
            {
                // Read one char at a time so we don't read past the end of the line.
                var task = _reader.ReadAsync(actual, offset, 1);
                if (!Debugger.IsAttached)
                {
                    Assert.True(task.Wait(4000), "timeout");
                }
                var count = await task.ConfigureAwait(false);
                if (count == 0)
                {
                    break;
                }

                Assert.True(count == 1);
                offset++;

                if (actual[offset - 1] == '\n')
                {
                    break;
                }
            }

            var actualLine = new string(actual, 0, offset);
            Assert.StartsWith(prefix, actualLine);
        }

        public void Shutdown(SocketShutdown how)
        {
            _socket.Shutdown(how);
        }

        public Task WaitForConnectionClose()
        {
            var tcs = new TaskCompletionSource<object>();
            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.SetBuffer(new byte[128], 0, 128);
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
            var tcs = (TaskCompletionSource<object>)e.UserToken;
            if (e.BytesTransferred == 0)
            {
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetException(new IOException(
                    $"Expected connection close, received data instead: \"{_reader.CurrentEncoding.GetString(e.Buffer, 0, e.BytesTransferred)}\""));
            }
        }

        public static Socket CreateConnectedLoopbackSocket(int port) => CreateConnectedLoopbackSocket(port, AddressFamily.InterNetwork);

        public static Socket CreateConnectedLoopbackSocket(int port, AddressFamily addressFamily)
        {
            if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException($"TestConnection does not support address family of type {addressFamily}", nameof(addressFamily));
            }

            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            var address = addressFamily == AddressFamily.InterNetworkV6
                ? IPAddress.IPv6Loopback
                : IPAddress.Loopback;
            socket.Connect(new IPEndPoint(address, port));
            return socket;
        }
    }
}
