// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal abstract class BrowserFileStream : Stream
    {
        private long _position;

        protected BrowserFile File { get; }

        protected BrowserFileStream(BrowserFile file)
        {
            File = file;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => File.Size;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Synchronous reads are not supported.");

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int maxBytesToRead = (int)(Length - Position);

            if (maxBytesToRead > buffer.Length)
            {
                maxBytesToRead = buffer.Length;
            }

            if (maxBytesToRead <= 0)
            {
                return 0;
            }

            var bytesRead = await CopyFileDataIntoBuffer(_position, buffer.Slice(0, maxBytesToRead), cancellationToken);

            _position += bytesRead;

            return bytesRead;
        }

        protected abstract ValueTask<int> CopyFileDataIntoBuffer(long sourceOffset, Memory<byte> destination, CancellationToken cancellationToken);
    }
}
