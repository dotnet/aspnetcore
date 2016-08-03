// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebSockets.Protocol.Test
{
    // A duplex wrapper around a read and write stream.
    public class DuplexStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public DuplexStream()
            : this (new BufferStream(), new BufferStream())
        {
        }

        public DuplexStream(Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
        }

        public DuplexStream CreateReverseDuplexStream()
        {
            return new DuplexStream(_writeStream, _readStream);
        }

#region Properties

        public override bool CanRead
        {
            get { return _readStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return _readStream.CanTimeout || _writeStream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return _writeStream.CanWrite; }
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
            get { return _readStream.ReadTimeout; }
            set { _readStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return _writeStream.WriteTimeout; }
            set { _writeStream.WriteTimeout = value; }
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

#region Read

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

#if !NETCOREAPP1_0
        public override int ReadByte()
        {
            return _readStream.ReadByte();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _readStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _readStream.EndRead(asyncResult);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _readStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }
#endif

#endregion Read

#region Write

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

#if !NETCOREAPP1_0
        public override void WriteByte(byte value)
        {
            _writeStream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _writeStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _writeStream.EndWrite(asyncResult);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeStream.FlushAsync(cancellationToken);
        }
#endif

        public override void Flush()
        {
            _writeStream.Flush();
        }

#endregion Write

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _readStream.Dispose();
                _writeStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
