// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression;

internal sealed class SizeLimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long? _sizeLimit;

    private long _totalBytesRead;

    public SizeLimitedStream(Stream innerStream, long? sizeLimit)
    {
        if (innerStream is null)
        {
            throw new ArgumentNullException(nameof(innerStream));
        }

        _innerStream = innerStream;
        _sizeLimit = sizeLimit;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _innerStream.Length;

    public override long Position
    {
        get
        {
            return _innerStream.Position;
        }
        set
        {
            _innerStream.Position = value;
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);

        _totalBytesRead += bytesRead;
        if (_totalBytesRead > _sizeLimit)
        {
            throw new InvalidOperationException("The maximum number of bytes have been read.");
        }

        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToApm.End<int>(asyncResult);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);

        _totalBytesRead += bytesRead;
        if (_totalBytesRead > _sizeLimit)
        {
            throw new InvalidOperationException("The maximum number of bytes have been read.");
        }

        return bytesRead;
    }
}
