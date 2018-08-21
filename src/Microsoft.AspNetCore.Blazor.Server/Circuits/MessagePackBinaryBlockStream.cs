// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MessagePack;
using System;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    /// <summary>
    /// A write-only stream that outputs its data to an underlying expandable
    /// buffer in the format for a MessagePack 'Bin32' block. Supports writing
    /// into buffers up to 2GB in length.
    /// </summary>
    internal class MessagePackBinaryBlockStream : Stream
    {
        // MessagePack Bin32 block
        // https://github.com/msgpack/msgpack/blob/master/spec.md#bin-format-family
        const int HeaderLength = 5;

        private byte[] _buffer;
        private int _headerStartOffset;
        private int _bodyLength;

        public MessagePackBinaryBlockStream(byte[] buffer, int offset)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _headerStartOffset = offset;
            _bodyLength = 0;

            // Leave space for header
            MessagePackBinary.EnsureCapacity(ref _buffer, _headerStartOffset, HeaderLength);
        }

        public byte[] Buffer => _buffer;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        // Length is the complete number of bytes being output, including header
        public override long Length => _bodyLength + HeaderLength;

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
            var outputOffset = _headerStartOffset + HeaderLength + _bodyLength;
            MessagePackBinary.EnsureCapacity(ref _buffer, outputOffset, count);
            System.Buffer.BlockCopy(src, srcOffset, _buffer, outputOffset, count);
            _bodyLength += count;
        }

        public override void Close()
        {
            // Write the header into the space we reserved at the beginning
            // This format matches the MessagePack spec
            unchecked
            {
                _buffer[_headerStartOffset] = MessagePackCode.Bin32;
                _buffer[_headerStartOffset + 1] = (byte)(_bodyLength >> 24);
                _buffer[_headerStartOffset + 2] = (byte)(_bodyLength >> 16);
                _buffer[_headerStartOffset + 3] = (byte)(_bodyLength >> 8);
                _buffer[_headerStartOffset + 4] = (byte)(_bodyLength);
            }
        }
    }
}
