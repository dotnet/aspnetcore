// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class SegmentWriteStream : Stream
    {
        private readonly List<byte[]> _segments = new List<byte[]>();
        private readonly MemoryStream _bufferStream = new MemoryStream();
        private readonly int _segmentSize;
        private long _length;
        private bool _closed;
        private bool _disposed;

        internal SegmentWriteStream(int segmentSize)
        {
            if (segmentSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentSize), segmentSize, $"{nameof(segmentSize)} must be greater than 0.");
            }

            _segmentSize = segmentSize;
        }

        // Extracting the buffered segments closes the stream for writing
        internal List<byte[]> GetSegments()
        {
            if (!_closed)
            {
                _closed = true;
                FinalizeSegments();
            }
            return _segments;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !_closed;

        public override long Length => _length;

        public override long Position
        {
            get
            {
                return _length;
            }
            set
            {
                throw new NotSupportedException("The stream does not support seeking.");
            }
        }

        private void DisposeMemoryStream()
        {
            // Clean up the memory stream
            _bufferStream.SetLength(0);
            _bufferStream.Capacity = 0;
            _bufferStream.Dispose();
        }

        private void FinalizeSegments()
        {
            // Append any remaining segments
            if (_bufferStream.Length > 0)
            {
                // Add the last segment
                _segments.Add(_bufferStream.ToArray());
            }

            DisposeMemoryStream();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    _segments.Clear();
                    DisposeMemoryStream();
                }

                _disposed = true;
                _closed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            if (!CanWrite)
            {
                throw new ObjectDisposedException("The stream has been closed for writing.");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("The stream does not support reading.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("The stream does not support seeking.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("The stream does not support seeking.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Non-negative number required.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Non-negative number required.");
            }
            if (count > buffer.Length - offset)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
            }
            if (!CanWrite)
            {
                throw new ObjectDisposedException("The stream has been closed for writing.");
            }

            while (count > 0)
            {
                if ((int)_bufferStream.Length == _segmentSize)
                {
                    _segments.Add(_bufferStream.ToArray());
                    _bufferStream.SetLength(0);
                }

                var bytesWritten = Math.Min(count, _segmentSize - (int)_bufferStream.Length);

                _bufferStream.Write(buffer, offset, bytesWritten);
                count -= bytesWritten;
                offset += bytesWritten;
                _length += bytesWritten;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override void WriteByte(byte value)
        {
            if (!CanWrite)
            {
                throw new ObjectDisposedException("The stream has been closed for writing.");
            }

            if ((int)_bufferStream.Length == _segmentSize)
            {
                _segments.Add(_bufferStream.ToArray());
                _bufferStream.SetLength(0);
            }

            _bufferStream.WriteByte(value);
            _length++;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return StreamUtilities.ToIAsyncResult(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            ((Task)asyncResult).GetAwaiter().GetResult();
        }
    }
}
