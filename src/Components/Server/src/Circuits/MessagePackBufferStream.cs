// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MessagePack;
using System;
using System.IO;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// Provides Stream APIs for writing to a MessagePack-supplied expandable buffer.
    /// </summary>
    internal class MessagePackBufferStream : Stream
    {
        private byte[] _buffer;
        private int _headerStartOffset;
        private int _bodyLength;

        public MessagePackBufferStream(byte[] buffer, int offset)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _headerStartOffset = offset;
            _bodyLength = 0;
        }

        public byte[] Buffer => _buffer;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        // Length is the complete number of bytes being output
        public override long Length => _bodyLength;

        // Position is the index into the writable body (i.e., so position zero
        // is the first byte you can actually write a value to)
        public override long Position
        {
            get => _bodyLength;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            // Nothing to do, as we're not buffering separately anyway
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotImplementedException();

        public override void SetLength(long value)
            => throw new NotImplementedException();

        public override void Write(byte[] src, int srcOffset, int count)
        {
            var outputOffset = _headerStartOffset + _bodyLength;
            MessagePackBinary.EnsureCapacity(ref _buffer, outputOffset, count);
            System.Buffer.BlockCopy(src, srcOffset, _buffer, outputOffset, count);
            _bodyLength += count;
        }
    }
}
