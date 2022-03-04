// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class SegmentReadStream : Stream
    {
        private readonly List<byte[]> _segments;
        private readonly long _length;
        private int _segmentIndex;
        private int _segmentOffset;
        private long _position;

        internal SegmentReadStream(List<byte[]> segments, long length)
        {
            _segments = segments ?? throw new ArgumentNullException(nameof(segments));
            _length = length;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                // The stream only supports a full rewind. This will need an update if random access becomes a required feature.
                if (value != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(Position)} can only be set to 0.");
                }

                _position = 0;
                _segmentOffset = 0;
                _segmentIndex = 0;
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException("The stream does not support writing.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Non-negative number required.");
            }
            // Read of length 0 will return zero and indicate end of stream.
            if (count <= 0 )
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Positive number required.");
            }
            if (count > buffer.Length - offset)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
            }

            if (_segmentIndex == _segments.Count)
            {
                return 0;
            }

            var bytesRead = 0;
            while (count > 0)
            {
                if (_segmentOffset == _segments[_segmentIndex].Length)
                {
                    // Move to the next segment
                    _segmentIndex++;
                    _segmentOffset = 0;

                    if (_segmentIndex == _segments.Count)
                    {
                        break;
                    }
                }

                // Read up to the end of the segment
                var segmentBytesRead = Math.Min(count, _segments[_segmentIndex].Length - _segmentOffset);
                Buffer.BlockCopy(_segments[_segmentIndex], _segmentOffset, buffer, offset, segmentBytesRead);
                bytesRead += segmentBytesRead;
                _segmentOffset += segmentBytesRead;
                _position += segmentBytesRead;
                offset += segmentBytesRead;
                count -= segmentBytesRead;
            }

            return bytesRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(Read(buffer, offset, count));
        }

        public override int ReadByte()
        {
            if (Position == Length)
            {
                return -1;
            }

            if (_segmentOffset == _segments[_segmentIndex].Length)
            {
                // Move to the next segment
                _segmentIndex++;
                _segmentOffset = 0;
            }

            var byteRead = _segments[_segmentIndex][_segmentOffset];
            _segmentOffset++;
            _position++;

            return byteRead;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);

            try
            {
                tcs.TrySetResult(Read(buffer, offset, count));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (callback != null)
            {
                // Offload callbacks to avoid stack dives on sync completions.
                var ignored = Task.Run(() =>
                {
                    try
                    {
                        callback(tcs.Task);
                    }
                    catch (Exception)
                    {
                        // Suppress exceptions on background threads.
                    }
                });
            }

            return tcs.Task;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            return ((Task<int>)asyncResult).GetAwaiter().GetResult();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // The stream only supports a full rewind. This will need an update if random access becomes a required feature.
            if (origin != SeekOrigin.Begin)
            {
                throw new ArgumentException(nameof(origin), $"{nameof(Seek)} can only be set to {nameof(SeekOrigin.Begin)}.");
            }
            if (offset != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(Seek)} can only be set to 0.");
            }

            Position = 0;
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("The stream does not support writing.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("The stream does not support writing.");
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (!destination.CanWrite)
            {
                throw new NotSupportedException("The destination stream does not support writing.");
            }

            for (; _segmentIndex < _segments.Count; _segmentIndex++, _segmentOffset = 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesCopied = _segments[_segmentIndex].Length - _segmentOffset;
                await destination.WriteAsync(_segments[_segmentIndex], _segmentOffset, bytesCopied, cancellationToken);
                _position += bytesCopied;
            }
        }
    }
}
