// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable


namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Stream that delegates to an inner stream.
/// This Stream is present so that the inner stream is not closed
/// even when Close() or Dispose() is called.
/// </summary>
internal class NonDisposableStream : Stream
{
    private readonly Stream _innerStream;

    /// <summary>
    /// Initializes a new <see cref="NonDisposableStream"/>.
    /// </summary>
    /// <param name="innerStream">The stream which should not be closed or flushed.</param>
    public NonDisposableStream(Stream innerStream)
    {
        if (innerStream == null)
        {
            throw new ArgumentNullException(nameof(innerStream));
        }

        _innerStream = innerStream;
    }

    /// <inheritdoc />
    public override bool CanRead => _innerStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _innerStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _innerStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _innerStream.Length;
    /// <inheritdoc />
    public override long Position
    {
        get { return _innerStream.Position; }
        set { _innerStream.Position = value; }
    }

    /// <inheritdoc />
    public override int ReadTimeout
    {
        get { return _innerStream.ReadTimeout; }
        set { _innerStream.ReadTimeout = value; }
    }

    /// <inheritdoc />
    public override bool CanTimeout => _innerStream.CanTimeout;

    /// <inheritdoc />
    public override int WriteTimeout
    {
        get { return _innerStream.WriteTimeout; }
        set { _innerStream.WriteTimeout = value; }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return _innerStream.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override IAsyncResult BeginRead(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state)
    {
        return _innerStream.BeginRead(buffer, offset, count, callback, state);
    }

    /// <inheritdoc />
    public override int EndRead(IAsyncResult asyncResult)
    {
        return _innerStream.EndRead(asyncResult);
    }

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(
        byte[] buffer,
        int offset,
        int count,
        AsyncCallback? callback,
        object? state)
    {
        return _innerStream.BeginWrite(buffer, offset, count, callback, state);
    }

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult)
    {
        _innerStream.EndWrite(asyncResult);
    }

    /// <inheritdoc />
    public override void Close()
    {
    }

    /// <inheritdoc />
    public override int ReadByte()
    {
        return _innerStream.ReadByte();
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // Do nothing, we want to explicitly avoid flush because it turns on Chunked encoding.
    }

    /// <inheritdoc />
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _innerStream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        _innerStream.WriteByte(value);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // No-op. In CoreCLR this is equivalent to Close.
        // Given that we don't own the underlying stream, we never want to do anything interesting here.
    }
}
