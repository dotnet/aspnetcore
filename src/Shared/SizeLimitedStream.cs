// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class SizeLimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long? _sizeLimit;

    private long _totalBytesRead;

    public SizeLimitedStream(Stream innerStream, long? sizeLimit)
    {
        ArgumentNullException.ThrowIfNull(innerStream);

        _innerStream = innerStream;
        _sizeLimit = sizeLimit;
    }

    public override bool CanRead => _innerStream.CanRead;

    public override bool CanSeek => _innerStream.CanSeek;

    public override bool CanWrite => _innerStream.CanWrite;

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
        _innerStream.Flush();
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
        _innerStream.Write(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
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
