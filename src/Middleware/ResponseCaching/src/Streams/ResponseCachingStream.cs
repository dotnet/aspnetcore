// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class ResponseCachingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxBufferSize;
        private readonly int _segmentSize;
        private readonly SegmentWriteStream _segmentWriteStream;
        private readonly Action _startResponseCallback;

        internal ResponseCachingStream(Stream innerStream, long maxBufferSize, int segmentSize, Action startResponseCallback)
        {
            _innerStream = innerStream;
            _maxBufferSize = maxBufferSize;
            _segmentSize = segmentSize;
            _startResponseCallback = startResponseCallback;
            _segmentWriteStream = new SegmentWriteStream(_segmentSize);
        }

        internal bool BufferingEnabled { get; private set; } = true;

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get { return _innerStream.Position; }
            set
            {
                DisableBuffering();
                _innerStream.Position = value;
            }
        }

        internal CachedResponseBody GetCachedResponseBody()
        {
            if (!BufferingEnabled)
            {
                throw new InvalidOperationException("Buffer stream cannot be retrieved since buffering is disabled.");
            }
            return new CachedResponseBody(_segmentWriteStream.GetSegments(), _segmentWriteStream.Length);
        }

        internal void DisableBuffering()
        {
            BufferingEnabled = false;
            _segmentWriteStream.Dispose();
        }

        public override void SetLength(long value)
        {
            DisableBuffering();
            _innerStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            DisableBuffering();
            return _innerStream.Seek(offset, origin);
        }

        public override void Flush()
        {
            try
            {
                _startResponseCallback();
                _innerStream.Flush();
            }
            catch
            {
                DisableBuffering();
                throw;
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            try
            {
                _startResponseCallback();
                await _innerStream.FlushAsync();
            }
            catch
            {
                DisableBuffering();
                throw;
            }
        }

        // Underlying stream is write-only, no need to override other read related methods
        public override int Read(byte[] buffer, int offset, int count)
            => _innerStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                _startResponseCallback();
                _innerStream.Write(buffer, offset, count);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (_segmentWriteStream.Length + count > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    _segmentWriteStream.Write(buffer, offset, count);
                }
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                _startResponseCallback();
                await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (_segmentWriteStream.Length + count > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    await _segmentWriteStream.WriteAsync(buffer, offset, count, cancellationToken);
                }
            }
        }

        public override void WriteByte(byte value)
        {
            try
            {
                _innerStream.WriteByte(value);
            }
            catch
            {
                DisableBuffering();
                throw;
            }

            if (BufferingEnabled)
            {
                if (_segmentWriteStream.Length + 1 > _maxBufferSize)
                {
                    DisableBuffering();
                }
                else
                {
                    _segmentWriteStream.WriteByte(value);
                }
            }
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
