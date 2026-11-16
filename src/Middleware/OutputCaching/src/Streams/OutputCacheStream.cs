// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCacheStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxBufferSize;
    private readonly int _segmentSize;
    private readonly RecyclableSequenceBuilder _segmentWriteStream;
    private readonly Action _startResponseCallback;
    private readonly bool _bufferResponseBody;

    internal OutputCacheStream(Stream innerStream, long maxBufferSize, int segmentSize, Action startResponseCallback, bool bufferResponseBody = false)
    {
        _innerStream = innerStream;
        _maxBufferSize = maxBufferSize;
        _segmentSize = segmentSize;
        _startResponseCallback = startResponseCallback;
        _bufferResponseBody = bufferResponseBody;
        _segmentWriteStream = new(_segmentSize);
    }

    internal bool BufferingEnabled { get; private set; } = true;

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get { return _innerStream.Position; }
        set
        {
            DisableBuffering();
            _innerStream.Position = value;
        }
    }

    internal ReadOnlySequence<byte> GetCachedResponseBody()
    {
        if (!BufferingEnabled)
        {
            throw new InvalidOperationException("Buffer stream cannot be retrieved since buffering is disabled.");
        }
        return _segmentWriteStream.DetachAndReset();
    }

    internal void DisableBuffering()
    {
        BufferingEnabled = false;
        _segmentWriteStream.Dispose();
    }

    // The buffered body is never sent to the client while buffering is enabled. Flush it
    // before disabling buffering so a response that exceeds MaximumBodySize is not lost.
    private void FlushBufferedResponseBody()
    {
        var buffered = _segmentWriteStream.DetachAndReset();
        foreach (var segment in buffered)
        {
            _innerStream.Write(segment.Span);
        }
    }

    private async ValueTask FlushBufferedResponseBodyAsync(CancellationToken cancellationToken)
    {
        var buffered = _segmentWriteStream.DetachAndReset();
        foreach (var segment in buffered)
        {
            await _innerStream.WriteAsync(segment, cancellationToken);
        }
    }

    public override void SetLength(long value)
    {
        DisableBuffering();
        _innerStream.SetLength(value);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        DisableBuffering();
        return _innerStream.Seek(offset, origin);
    }

    public override void Flush()
    {
        try
        {
            _startResponseCallback();
            _innerStream.Flush();
        }
        catch
        {
            DisableBuffering();
            throw;
        }
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        try
        {
            _startResponseCallback();
            await _innerStream.FlushAsync(cancellationToken);
        }
        catch
        {
            DisableBuffering();
            throw;
        }
    }

    // Underlying stream is write-only, no need to override other read related methods
    public override int Read(byte[] buffer, int offset, int count)
        => _innerStream.Read(buffer, offset, count);

    public override void Write(byte[] buffer, int offset, int count)
    {
        try
        {
            _startResponseCallback();
            if (!_bufferResponseBody || !BufferingEnabled)
            {
                _innerStream.Write(buffer, offset, count);
            }
            else if (_segmentWriteStream.Length + count > _maxBufferSize)
            {
                FlushBufferedResponseBody();
                _innerStream.Write(buffer, offset, count);
                DisableBuffering();
                return;
            }
        }
        catch
        {
            DisableBuffering();
            throw;
        }

        if (BufferingEnabled)
        {
            if (_segmentWriteStream.Length + count > _maxBufferSize)
            {
                DisableBuffering();
            }
            else
            {
                _segmentWriteStream.Write(buffer.AsSpan(offset, count));
            }
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        try
        {
            _startResponseCallback();
            if (!_bufferResponseBody || !BufferingEnabled)
            {
                _innerStream.Write(buffer);
            }
            else if (_segmentWriteStream.Length + buffer.Length > _maxBufferSize)
            {
                FlushBufferedResponseBody();
                _innerStream.Write(buffer);
                DisableBuffering();
                return;
            }
        }
        catch
        {
            DisableBuffering();
            throw;
        }

        if (BufferingEnabled)
        {
            if (_segmentWriteStream.Length + buffer.Length > _maxBufferSize)
            {
                DisableBuffering();
            }
            else
            {
                _segmentWriteStream.Write(buffer);
            }
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        await WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            _startResponseCallback();
            if (!_bufferResponseBody || !BufferingEnabled)
            {
                await _innerStream.WriteAsync(buffer, cancellationToken);
            }
            else if (_segmentWriteStream.Length + buffer.Length > _maxBufferSize)
            {
                await FlushBufferedResponseBodyAsync(cancellationToken);
                await _innerStream.WriteAsync(buffer, cancellationToken);
                DisableBuffering();
                return;
            }
        }
        catch
        {
            DisableBuffering();
            throw;
        }

        if (BufferingEnabled)
        {
            if (_segmentWriteStream.Length + buffer.Length > _maxBufferSize)
            {
                DisableBuffering();
            }
            else
            {
                _segmentWriteStream.Write(buffer.Span);
            }
        }
    }

    public override void WriteByte(byte value)
    {
        try
        {
            if (!_bufferResponseBody || !BufferingEnabled)
            {
                _innerStream.WriteByte(value);
            }
            else if (_segmentWriteStream.Length + 1 > _maxBufferSize)
            {
                FlushBufferedResponseBody();
                _innerStream.WriteByte(value);
                DisableBuffering();
                return;
            }
        }
        catch
        {
            DisableBuffering();
            throw;
        }

        if (BufferingEnabled)
        {
            if (_segmentWriteStream.Length + 1 > _maxBufferSize)
            {
                DisableBuffering();
            }
            else
            {
                _segmentWriteStream.WriteByte(value);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _segmentWriteStream?.Dispose();
        }
        base.Dispose(disposing);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

    public override void EndWrite(IAsyncResult asyncResult)
        => TaskToAsyncResult.End(asyncResult);
}
