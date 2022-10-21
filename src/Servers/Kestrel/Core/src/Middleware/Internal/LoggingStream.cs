// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class LoggingStream : Stream
{
    private readonly Stream _inner;
    private readonly ILogger _logger;

    public LoggingStream(Stream inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public override bool CanRead
    {
        get
        {
            return _inner.CanRead;
        }
    }

    public override bool CanSeek
    {
        get
        {
            return _inner.CanSeek;
        }
    }

    public override bool CanWrite
    {
        get
        {
            return _inner.CanWrite;
        }
    }

    public override long Length
    {
        get
        {
            return _inner.Length;
        }
    }

    public override long Position
    {
        get
        {
            return _inner.Position;
        }

        set
        {
            _inner.Position = value;
        }
    }

    public override void Flush()
    {
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = _inner.Read(buffer, offset, count);
        if (count > 0)
        {
            Log("Read", new ReadOnlySpan<byte>(buffer, offset, read));
        }
        return read;
    }

    public override int Read(Span<byte> destination)
    {
        int read = _inner.Read(destination);
        if (!destination.IsEmpty)
        {
            Log("Read", destination.Slice(0, read));
        }
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        if (count > 0)
        {
            Log("ReadAsync", new ReadOnlySpan<byte>(buffer, offset, read));
        }
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        int read = await _inner.ReadAsync(destination, cancellationToken);
        if (!destination.IsEmpty)
        {
            Log("ReadAsync", destination.Span.Slice(0, read));
        }
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Log("Write", new ReadOnlySpan<byte>(buffer, offset, count));
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> source)
    {
        Log("Write", source);
        _inner.Write(source);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Log("WriteAsync", new ReadOnlySpan<byte>(buffer, offset, count));
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        Log("WriteAsync", source.Span);
        return _inner.WriteAsync(source, cancellationToken);
    }

    private void Log(string method, ReadOnlySpan<byte> buffer)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append(method);
        builder.Append('[');
        builder.Append(buffer.Length);
        builder.Append(']');

        if (buffer.Length > 0)
        {
            builder.AppendLine();
        }

        // A maximum of 2 8 byte segments are written at once with a space between them, meaning 17 characters can be written
        // ........ ........
        Span<char> charBuilder = stackalloc char[17];
        var charBuilderIndex = 0;

        // Write the hex
        for (int i = 0; i < buffer.Length; i++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{buffer[i]:X2}");
            builder.Append(' ');

            var bufferChar = (char)buffer[i];
            if (char.IsControl(bufferChar))
            {
                charBuilder[charBuilderIndex++] = '.';
            }
            else
            {
                charBuilder[charBuilderIndex++] = bufferChar;
            }

            if ((i + 1) % 16 == 0)
            {
                builder.Append("  ");
                builder.Append(charBuilder);
                if (i != buffer.Length - 1)
                {
                    builder.AppendLine();
                }
                charBuilder.Clear();
                charBuilderIndex = 0;
            }
            else if ((i + 1) % 8 == 0)
            {
                builder.Append(' ');
                charBuilder[charBuilderIndex++] = ' ';
            }
        }

        // Different than charBuffer.Length since charBuffer contains an extra " " after the 8th byte.
        var numBytesInLastLine = buffer.Length % 16;

        if (numBytesInLastLine > 0)
        {
            // 2 (between hex and char blocks) + num bytes left (3 per byte)
            var padLength = 2 + (3 * (16 - numBytesInLastLine));
            // extra for space after 8th byte
            if (numBytesInLastLine < 8)
            {
                padLength++;
            }

            builder.Append(' ', padLength);
            builder.Append(charBuilder.Slice(0, charBuilderIndex));
        }

        _logger.LogDebug(builder.ToString());
    }

    // The below APM methods call the underlying Read/WriteAsync methods which will still be logged.
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToApm.End<int>(asyncResult);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        TaskToApm.End(asyncResult);
    }
}
