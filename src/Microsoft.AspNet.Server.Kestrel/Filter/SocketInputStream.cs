// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    /// <summary>
    /// This is a write-only stream that copies what is written into a
    /// <see cref="SocketInput"/> object. This is used as an argument to
    /// <see cref="Stream.CopyToAsync(Stream)" /> so input filtered by a
    /// ConnectionFilter (e.g. SslStream) can be consumed by <see cref="Frame"/>.
    /// </summary>
    public class SocketInputStream : Stream
    {
        private static Task _emptyTask = Task.FromResult<object>(null);
        private static byte[] _emptyBuffer = new byte[0];

        private readonly SocketInput _socketInput;

        public SocketInputStream(SocketInput socketInput)
        {
            _socketInput = socketInput;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            // No-op
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var inputBuffer = _socketInput.IncomingStart(count);

            Buffer.BlockCopy(buffer, offset, inputBuffer.Data.Array, inputBuffer.Data.Offset, count);

            _socketInput.IncomingComplete(count, error: null);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            Write(buffer, offset, count);
            return _emptyTask;
        }

        protected override void Dispose(bool disposing)
        {
            // Close _socketInput with a 0-length write.
            Write(_emptyBuffer, 0, 0);
            base.Dispose(disposing);
        }
    }
}
