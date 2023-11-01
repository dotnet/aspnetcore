// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

/// <summary>
/// Summary description for TestConnection
/// </summary>
public class TestConnection : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    private readonly bool _ownsSocket;
    private readonly Socket _socket;
    private readonly NetworkStream _stream;

    public TestConnection(int port)
        : this(port, AddressFamily.InterNetwork)
    {
    }

    public TestConnection(int port, AddressFamily addressFamily)
        : this(CreateConnectedLoopbackSocket(port, addressFamily), ownsSocket: true)
    {
    }

    public TestConnection(Socket socket)
        : this(socket, ownsSocket: false)
    {
    }

    private TestConnection(Socket socket, bool ownsSocket)
    {
        _ownsSocket = ownsSocket;
        _socket = socket;
        _stream = new NetworkStream(_socket, ownsSocket: false);
    }

    public Socket Socket => _socket;
    public Stream Stream => _stream;

    public void Dispose()
    {
        _stream.Dispose();

        if (_ownsSocket)
        {
            _socket.Dispose();
        }
    }

    public async Task Send(params string[] lines)
    {
        var bytes = Encoding.ASCII.GetBytes(string.Join("\r\n", lines));

        for (var index = 0; index < bytes.Length; index++)
        {
            await _stream.WriteAsync(bytes, index, 1).ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
            // Re-add delay to help find socket input consumption bugs more consistently
            //await Task.Delay(TimeSpan.FromMilliseconds(5));
        }
    }

    public async Task<int> ReadCharAsync()
    {
        var bytes = new byte[1];
        return (await _stream.ReadAsync(bytes, 0, 1) == 1) ? bytes[0] : -1;
    }

    public async Task<string> ReadLineAsync()
    {
        var builder = new StringBuilder();
        var current = await ReadCharAsync();
        while (current != '\r')
        {
            builder.Append((char)current);
            current = await ReadCharAsync();
        }

        // Consume \n
        await ReadCharAsync();

        return builder.ToString();
    }

    public async Task<Memory<byte>> ReceiveChunk()
    {
        var length = int.Parse(await ReadLineAsync(), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        var bytes = await Receive(length);

        await ReadLineAsync();

        return bytes;
    }

    public async Task ReceiveChunk(string expected)
    {
        Assert.Equal(expected, Encoding.ASCII.GetString((await ReceiveChunk()).Span));
    }

    public async Task Receive(params string[] lines)
    {
        var expected = string.Join("\r\n", lines);
        var actual = await Receive(expected.Length);

        Assert.Equal(expected, Encoding.ASCII.GetString(actual.Span));
    }

    public async Task<Memory<byte>> Receive(int length)
    {
        var actual = new byte[length];
        int offset = 0;
        try
        {
            while (offset < length)
            {
                var task = _stream.ReadAsync(actual, offset, actual.Length - offset);
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
        }
        catch (TimeoutException ex) when (offset != 0)
        {
            throw new TimeoutException(
                $"Did not receive a complete response within {Timeout}.{Environment.NewLine}{Environment.NewLine}" +
                $"Expected:{Environment.NewLine}{length} bytes of data{Environment.NewLine}{Environment.NewLine}" +
                $"Actual:{Environment.NewLine}{Encoding.ASCII.GetString(actual, 0, offset)}{Environment.NewLine}",
                ex);
        }

        return actual.AsMemory(0, offset);
    }

    public async Task ReceiveStartsWith(string prefix, int maxLineLength = 1024)
    {
        var actual = new byte[maxLineLength];
        var offset = 0;

        while (offset < maxLineLength)
        {
            // Read one char at a time so we don't read past the end of the line.
            var task = _stream.ReadAsync(actual, offset, 1);
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

        var actualLine = Encoding.ASCII.GetString(actual, 0, offset);
        Assert.StartsWith(prefix, actualLine);
    }

    public async Task<string[]> ReceiveHeaders(params string[] lines)
    {
        List<string> headers = new List<string>();
        string line;
        do
        {
            line = await ReadLineAsync();
            headers.Add(line);
        } while (line != "");

        foreach (var s in lines)
        {
            Assert.Contains(s, headers);
        }

        return headers.ToArray();
    }

    public Task WaitForConnectionClose()
    {
        var tcs = new TaskCompletionSource();
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
        var tcs = (TaskCompletionSource)e.UserToken;
        if (e.BytesTransferred == 0)
        {
            tcs.SetResult();
        }
        else
        {
            tcs.SetException(new IOException(
                $"Expected connection close, received data instead: \"{Encoding.ASCII.GetString(e.Buffer, 0, e.BytesTransferred)}\""));
        }
    }

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
