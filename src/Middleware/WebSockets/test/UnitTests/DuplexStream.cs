// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebSockets.Test;

// A duplex wrapper around a read and write stream.
public class DuplexStream : Stream
{
    public BufferStream ReadStream { get; }
    public BufferStream WriteStream { get; }

    public DuplexStream()
        : this(new BufferStream(), new BufferStream())
    {
    }

    public DuplexStream(BufferStream readStream, BufferStream writeStream)
    {
        ReadStream = readStream;
        WriteStream = writeStream;
    }

    public DuplexStream CreateReverseDuplexStream()
    {
        return new DuplexStream(WriteStream, ReadStream);
    }

    #region Properties

    public override bool CanRead
    {
        get { return ReadStream.CanRead; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanTimeout
    {
        get { return ReadStream.CanTimeout || WriteStream.CanTimeout; }
    }

    public override bool CanWrite
    {
        get { return WriteStream.CanWrite; }
    }

    public override long Length
    {
        get { throw new NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public override int ReadTimeout
    {
        get { return ReadStream.ReadTimeout; }
        set { ReadStream.ReadTimeout = value; }
    }

    public override int WriteTimeout
    {
        get { return WriteStream.WriteTimeout; }
        set { WriteStream.WriteTimeout = value; }
    }

    #endregion Properties

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override int ReadByte()
    {
        return ReadStream.ReadByte();
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        return ReadStream.BeginRead(buffer, offset, count, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return ReadStream.EndRead(asyncResult);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return ReadStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    #region Read

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadStream.Read(buffer, offset, count);
    }

    #endregion Read

    #region Write

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteStream.Write(buffer, offset, count);
    }
    public override void WriteByte(byte value)
    {
        WriteStream.WriteByte(value);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        return WriteStream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        WriteStream.EndWrite(asyncResult);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return WriteStream.FlushAsync(cancellationToken);
    }

    public override void Flush()
    {
        WriteStream.Flush();
    }

    #endregion Write

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReadStream.Dispose();
            WriteStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
