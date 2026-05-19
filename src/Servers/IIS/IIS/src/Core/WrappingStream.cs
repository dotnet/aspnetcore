// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class WrappingStream : Stream
{
    private Stream _inner;
    private bool _disposed;

    public WrappingStream(Stream inner)
    {
        _inner = inner;
    }

    public void SetInnerStream(Stream inner)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _inner = inner;
    }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => _inner.CanSeek;

    public override bool CanWrite => _inner.CanWrite;

    public override bool CanTimeout => _inner.CanTimeout;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int ReadTimeout
    {
        get => _inner.ReadTimeout;
        set => _inner.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => _inner.WriteTimeout;
        set => _inner.WriteTimeout = value;
    }

    public override void Flush()
        => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
        => _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        => _inner.ReadAsync(destination, cancellationToken);

    public override int ReadByte()
        => _inner.ReadByte();

    public override long Seek(long offset, SeekOrigin origin)
        => _inner.Seek(offset, origin);

    public override void SetLength(long value)
        => _inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => _inner.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(source, cancellationToken);

    public override void WriteByte(byte value)
        => _inner.WriteByte(value);

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        => _inner.CopyToAsync(destination, bufferSize, cancellationToken);

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _inner.BeginRead(buffer, offset, count, callback, state);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => _inner.BeginWrite(buffer, offset, count, callback, state);

    public override int EndRead(IAsyncResult asyncResult)
        => _inner.EndRead(asyncResult);

    public override void EndWrite(IAsyncResult asyncResult)
        => _inner.EndWrite(asyncResult);

#pragma warning disable CS0672, SYSLIB0010 // Overrides obsolete member
    public override object InitializeLifetimeService()
        => _inner.InitializeLifetimeService();
#pragma warning restore CS0672, SYSLIB0010 // Overrides obsolete member

    public override void Close()
        => _inner.Close();

    public override bool Equals(object? obj)
        => _inner.Equals(obj);

    public override int GetHashCode()
        => _inner.GetHashCode();

    public override string? ToString()
        => _inner.ToString();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            _inner.Dispose();
        }
    }
}
