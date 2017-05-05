// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    // A duplex wrapper around a read and write stream.
    public class DuplexStream : Stream
    {
        public BufferStream ReadStream { get; }
        public BufferStream WriteStream { get; }

        public DuplexStream()
            : this (new BufferStream(), new BufferStream())
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
}
