// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebUtilities
{
    internal class MultipartReaderStream : Stream
    {
        private readonly BufferedReadStream _innerStream;
        private readonly byte[] _boundaryBytes;
        private readonly int _finalBoundaryLength;
        private readonly long _innerOffset;
        private long _position;
        private long _observedLength;
        private bool _finished;

        /// <summary>
        /// Creates a stream that reads until it reaches the given boundary pattern.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="boundary"></param>
        public MultipartReaderStream(BufferedReadStream stream, string boundary, bool expectLeadingCrlf = true)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (boundary == null)
            {
                throw new ArgumentNullException(nameof(boundary));
            }

            _innerStream = stream;
            _innerOffset = _innerStream.CanSeek ? _innerStream.Position : 0;
            if (expectLeadingCrlf)
            {
                _boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary);
            }
            else
            {
                _boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);
            }
            _finalBoundaryLength = _boundaryBytes.Length + 2; // Include the final '--' terminator.
        }

        public bool FinalBoundaryFound { get; private set; }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return _innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _observedLength; }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Position must be positive.");
                }
                if (value > _observedLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The Position must be less than length.");
                }
                _position = value;
                if (_position < _observedLength)
                {
                    _finished = false;
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                Position = Position + offset;
            }
            else // if (origin == SeekOrigin.End)
            {
                Position = Length + offset;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
#if NET451
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }
#endif
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        private void PositionInnerStream()
        {
            if (_innerStream.CanSeek && _innerStream.Position != (_innerOffset + _position))
            {
                _innerStream.Position = _innerOffset + _position;
            }
        }

        private int UpdatePosition(int read)
        {
            _position += read;
            if (_observedLength < _position)
            {
                _observedLength = _position;
            }
            return read;
        }
#if NET451
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            InternalReadAsync(buffer, offset, size, callback, tcs);
            return tcs.Task;
        }

        private async void InternalReadAsync(byte[] buffer, int offset, int size, AsyncCallback callback, TaskCompletionSource<int> tcs)
        {
            try
            {
                int read = await ReadAsync(buffer, offset, size);
                tcs.TrySetResult(read);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (callback != null)
            {
                try
                {
                    callback(tcs.Task);
                }
                catch (Exception)
                {
                    // Suppress exceptions on background threads.
                }
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var task = (Task<int>)asyncResult;
            return task.GetAwaiter().GetResult();
        }
#endif
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_finished)
            {
                return 0;
            }

            PositionInnerStream();
            if (!_innerStream.EnsureBuffered(_finalBoundaryLength))
            {
                throw new IOException("Unexpected end of stream.");
            }
            var bufferedData = _innerStream.BufferedData;

            // scan for a boundary match, full or partial.
            int matchOffset;
            int matchCount;
            int read;
            if (SubMatch(bufferedData, _boundaryBytes, out matchOffset, out matchCount))
            {
                // We found a possible match, return any data before it.
                if (matchOffset > bufferedData.Offset)
                {
                    read = _innerStream.Read(buffer, offset, Math.Min(count, matchOffset - bufferedData.Offset));
                    return UpdatePosition(read);
                }
                Debug.Assert(matchCount == _boundaryBytes.Length);

                // "The boundary may be followed by zero or more characters of
                // linear whitespace. It is then terminated by either another CRLF"
                // or -- for the final boundary.
                byte[] boundary = new byte[_boundaryBytes.Length];
                read = _innerStream.Read(boundary, 0, boundary.Length);
                Debug.Assert(read == boundary.Length); // It should have all been buffered
                var remainder = _innerStream.ReadLine(lengthLimit: 100); // Whitespace may exceed the buffer.
                remainder = remainder.Trim();
                if (string.Equals("--", remainder, StringComparison.Ordinal))
                {
                    FinalBoundaryFound = true;
                }
                Debug.Assert(FinalBoundaryFound || string.Equals(string.Empty, remainder, StringComparison.Ordinal), "Un-expected data found on the boundary line: " + remainder);

                _finished = true;
                return 0;
            }

            // No possible boundary match within the buffered data, return the data from the buffer.
            read = _innerStream.Read(buffer, offset, Math.Min(count, bufferedData.Count));
            return UpdatePosition(read);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_finished)
            {
                return 0;
            }

            PositionInnerStream();
            if (!await _innerStream.EnsureBufferedAsync(_finalBoundaryLength, cancellationToken))
            {
                throw new IOException("Unexpected end of stream.");
            }
            var bufferedData = _innerStream.BufferedData;

            // scan for a boundary match, full or partial.
            int matchOffset;
            int matchCount;
            int read;
            if (SubMatch(bufferedData, _boundaryBytes, out matchOffset, out matchCount))
            {
                // We found a possible match, return any data before it.
                if (matchOffset > bufferedData.Offset)
                {
                    // Sync, it's already buffered
                    read = _innerStream.Read(buffer, offset, Math.Min(count, matchOffset - bufferedData.Offset));
                    return UpdatePosition(read);
                }
                Debug.Assert(matchCount == _boundaryBytes.Length);

                // "The boundary may be followed by zero or more characters of
                // linear whitespace. It is then terminated by either another CRLF"
                // or -- for the final boundary.
                byte[] boundary = new byte[_boundaryBytes.Length];
                read = _innerStream.Read(boundary, 0, boundary.Length);
                Debug.Assert(read == boundary.Length); // It should have all been buffered
                var remainder = await _innerStream.ReadLineAsync(lengthLimit: 100, cancellationToken: cancellationToken); // Whitespace may exceed the buffer.
                remainder = remainder.Trim();
                if (string.Equals("--", remainder, StringComparison.Ordinal))
                {
                    FinalBoundaryFound = true;
                }
                Debug.Assert(FinalBoundaryFound || string.Equals(string.Empty, remainder, StringComparison.Ordinal), "Un-expected data found on the boundary line: " + remainder);

                _finished = true;
                return 0;
            }

            // No possible boundary match within the buffered data, return the data from the buffer.
            read = _innerStream.Read(buffer, offset, Math.Min(count, bufferedData.Count));
            return UpdatePosition(read);
        }

        // Does Segment1 contain all of segment2, or does it end with the start of segment2?
        // 1: AAAAABBBBBCCCCC
        // 2:      BBBBB
        // Or:
        // 1: AAAAABBB
        // 2:      BBBBB
        private static bool SubMatch(ArraySegment<byte> segment1, byte[] matchBytes, out int matchOffset, out int matchCount)
        {
            matchCount = 0;
            for (matchOffset = segment1.Offset; matchOffset < segment1.Offset + segment1.Count; matchOffset++)
            {
                int countLimit = segment1.Offset - matchOffset + segment1.Count;
                for (matchCount = 0; matchCount < matchBytes.Length && matchCount < countLimit; matchCount++)
                {
                    if (matchBytes[matchCount] != segment1.Array[matchOffset + matchCount])
                    {
                        matchCount = 0;
                        break;
                    }
                }
                if (matchCount > 0)
                {
                    break;
                }
            }
            return matchCount > 0;
        }
    }
}
