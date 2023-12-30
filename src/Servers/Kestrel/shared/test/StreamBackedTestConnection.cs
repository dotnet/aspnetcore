// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Summary description for TestConnection
/// </summary>
public abstract class StreamBackedTestConnection : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);

    private readonly Stream _stream;
    private readonly StreamReader _reader;

    protected StreamBackedTestConnection(Stream stream, Encoding encoding = null)
    {
        _stream = stream;
        _reader = new StreamReader(_stream, encoding ?? Encoding.ASCII);
    }

    public Stream Stream => _stream;

    public StreamReader Reader => _reader;

    public abstract void ShutdownSend();

    public abstract void Reset();

    public virtual void Dispose()
    {
        _stream.Dispose();
    }

    public Task SendEmptyGet()
    {
        return Send("GET / HTTP/1.1",
            "Host:",
            "",
            "");
    }

    public Task SendEmptyGetWithUpgradeAndKeepAlive()
        => SendEmptyGetWithConnection("Upgrade, keep-alive");

    public Task SendEmptyGetWithUpgrade()
        => SendEmptyGetWithConnection("Upgrade");

    public Task SendEmptyGetAsKeepAlive()
        => SendEmptyGetWithConnection("keep-alive");

    private Task SendEmptyGetWithConnection(string connection)
    {
        return Send("GET / HTTP/1.1",
            "Host:",
            "Connection: " + connection,
            "",
            "");
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

        try
        {
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
        }
        catch (TimeoutException ex) when (offset != 0)
        {
            throw new TimeoutException($"Did not receive a complete response within {Timeout}.{Environment.NewLine}{Environment.NewLine}" +
                $"Expected:{Environment.NewLine}{expected}{Environment.NewLine}{Environment.NewLine}" +
                $"Actual:{Environment.NewLine}{new string(actual, 0, offset)}{Environment.NewLine}",
                ex);
        }

        var actualText = new string(actual, 0, offset);
        Assert.Equal(expected, actualText);
    }

    public async Task ReceiveEnd(params string[] lines)
    {
        await Receive(lines).ConfigureAwait(false);
        var ch = new char[128];
        var count = await _reader.ReadAsync(ch, 0, 128).TimeoutAfter(Timeout).ConfigureAwait(false);
        var text = new string(ch, 0, count);
        Assert.Equal("", text);
    }

    public async Task WaitForConnectionClose()
    {
        var buffer = new byte[128];
        var bytesTransferred = await _stream.ReadAsync(buffer, 0, 128).ContinueWith(t => t.IsFaulted ? 0 : t.Result).TimeoutAfter(Timeout);

        if (bytesTransferred > 0)
        {
            throw new IOException(
                $"Expected connection close, received data instead: \"{_reader.CurrentEncoding.GetString(buffer, 0, bytesTransferred)}\"");
        }
    }
}
