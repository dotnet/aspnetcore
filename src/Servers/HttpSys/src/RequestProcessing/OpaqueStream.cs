// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

// A duplex wrapper around RequestStream and ResponseStream.
// TODO: Consider merging RequestStream and ResponseStream instead.
internal sealed class OpaqueStream : Stream
{
    private readonly Stream _requestStream;
    private readonly Stream _responseStream;

    internal OpaqueStream(Stream requestStream, Stream responseStream)
    {
        _requestStream = requestStream;
        _responseStream = responseStream;
    }

    #region Properties

    public override bool CanRead
    {
        get { return _requestStream.CanRead; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanTimeout
    {
        get { return _requestStream.CanTimeout || _responseStream.CanTimeout; }
    }

    public override bool CanWrite
    {
        get { return _responseStream.CanWrite; }
    }

    public override long Length
    {
        get { throw new NotSupportedException(Resources.Exception_NoSeek); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(Resources.Exception_NoSeek); }
        set { throw new NotSupportedException(Resources.Exception_NoSeek); }
    }

    public override int ReadTimeout
    {
        get { return _requestStream.ReadTimeout; }
        set { _requestStream.ReadTimeout = value; }
    }

    public override int WriteTimeout
    {
        get { return _responseStream.WriteTimeout; }
        set { _responseStream.WriteTimeout = value; }
    }

    #endregion Properties

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException(Resources.Exception_NoSeek);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException(Resources.Exception_NoSeek);
    }

    #region Read

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _requestStream.Read(buffer, offset, count);
    }

    public override int ReadByte()
    {
        return _requestStream.ReadByte();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _requestStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return _requestStream.EndRead(asyncResult);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _requestStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _requestStream.ReadAsync(buffer, cancellationToken);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _requestStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    #endregion Read

    #region Write

    public override void Write(byte[] buffer, int offset, int count)
    {
        _responseStream.Write(buffer, offset, count);
    }

    public override void WriteByte(byte value)
    {
        _responseStream.WriteByte(value);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return _responseStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        _responseStream.EndWrite(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _responseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _responseStream.WriteAsync(buffer, cancellationToken);
    }

    public override void Flush()
    {
        _responseStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _responseStream.FlushAsync(cancellationToken);
    }

    #endregion Write

    protected override void Dispose(bool disposing)
    {
        // TODO: Suppress dispose?
        if (disposing)
        {
            _requestStream.Dispose();
            _responseStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
