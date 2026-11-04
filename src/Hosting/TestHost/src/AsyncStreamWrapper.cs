// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.TestHost;

internal sealed class AsyncStreamWrapper : Stream
{
    private readonly Stream _inner;
    private readonly Func<bool> _allowSynchronousIO;

    internal AsyncStreamWrapper(Stream inner, Func<bool> allowSynchronousIO)
    {
        _inner = inner;
        _allowSynchronousIO = allowSynchronousIO;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => throw new NotSupportedException("The stream is not seekable.");

    public override long Position
    {
        get => throw new NotSupportedException("The stream is not seekable.");
        set => throw new NotSupportedException("The stream is not seekable.");
    }

    public override void Flush()
    {
        // Not blocking Flush because things like StreamWriter.Dispose() always call it.
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _inner.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_allowSynchronousIO())
        {
            throw new InvalidOperationException("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true.");
        }

        return _inner.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _inner.ReadAsync(buffer, cancellationToken);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _inner.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _inner.EndRead(asyncResult);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("The stream is not seekable.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("The stream is not seekable.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!_allowSynchronousIO())
        {
            throw new InvalidOperationException("Synchronous operations are disallowed. Call WriteAsync or set AllowSynchronousIO to true.");
        }

        _inner.Write(buffer, offset, count);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _inner.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _inner.EndWrite(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    public override void Close()
    {
        // Don't dispose the inner stream, we don't want to impact the client stream
    }

    protected override void Dispose(bool disposing)
    {
        // Don't dispose the inner stream, we don't want to impact the client stream
    }

    public override ValueTask DisposeAsync()
    {
        // Don't dispose the inner stream, we don't want to impact the client stream
        return default;
    }
}
