// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    /// <summary>
    /// This is a write-only stream that copies what is written into a
    /// <see cref="SocketInput"/> object. This is used as an argument to
    /// <see cref="Stream.CopyToAsync(Stream)" /> so input filtered by a
    /// ConnectionFilter (e.g. SslStream) can be consumed by <see cref="Frame"/>.
    /// </summary>
    public class SocketInputStream : Stream
    {
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
            _socketInput.IncomingData(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            Write(buffer, offset, count);
            return TaskUtilities.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            // Close _socketInput with a fake zero-length write that will result in a zero-length read.
            _socketInput.IncomingData(null, 0, 0);
            base.Dispose(disposing);
        }
    }
}
