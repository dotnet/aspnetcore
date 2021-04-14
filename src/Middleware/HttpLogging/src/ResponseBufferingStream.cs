// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class ResponseBufferingStream : Stream, IHttpResponseBodyFeature, IBufferWriter<byte>
    {
        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;
        private readonly int _limit;
        private PipeWriter? _pipeAdapter;

        private readonly int _minimumSegmentSize;
        private int _bytesWritten;
        private List<CompletedBuffer>? _completedSegments;
        private byte[]? _currentSegment;
        private int _position;

        internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature, int limit)
        {
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _limit = limit;
            _minimumSegmentSize = 4096;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public Stream Stream => this;

        public PipeWriter Writer
        {
            get
            {
                if (_pipeAdapter == null)
                {
                    _pipeAdapter = PipeWriter.Create(Stream, new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _pipeAdapter;
            }
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

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            var tcs = new TaskCompletionSource(state: state, TaskCreationOptions.RunContinuationsAsynchronously);
            InternalWriteAsync(buffer, offset, count, callback, tcs);
            return tcs.Task;
        }

        private async void InternalWriteAsync(byte[] buffer, int offset, int count, AsyncCallback? callback, TaskCompletionSource tcs)
        {
            try
            {
                await WriteAsync(buffer, offset, count);
                tcs.TrySetResult();
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
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var task = (Task)asyncResult;
            task.GetAwaiter().GetResult();
        }

#if NETCOREAPP
        public override void Write(ReadOnlySpan<byte> span)
        {
            var remaining = _limit - _bytesWritten;
            var innerCount = Math.Min(remaining, span.Length);

            if (_currentSegment != null && span.Slice(0, innerCount).TryCopyTo(_currentSegment.AsSpan(_position)))
            {
                _position += innerCount;
                _bytesWritten += innerCount;
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
            }

            _innerStream.Write(span);
        }
#endif

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = _limit - _bytesWritten;
            var innerCount = Math.Min(remaining, count);

            var position = _position;
            if (_currentSegment != null && position < _currentSegment.Length - innerCount)
            {
                Buffer.BlockCopy(buffer, offset, _currentSegment, position, innerCount);

                _position = position + innerCount;
                _bytesWritten += innerCount;
            }
            else
            {
                BuffersExtensions.Write(this, buffer.AsSpan(offset, innerCount));
            }

            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void DisableBuffering()
        {
            _innerBodyFeature.DisableBuffering();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            return _innerBodyFeature.SendFileAsync(path, offset, count, cancellation);
        }

        public Task StartAsync(CancellationToken token = default)
        {
            return _innerBodyFeature.StartAsync(token);
        }

        public async Task CompleteAsync()
        {
            await _innerBodyFeature.CompleteAsync();
        }

        // IBufferWriter<byte>
        public void Reset()
        {
            if (_completedSegments != null)
            {
                for (var i = 0; i < _completedSegments.Count; i++)
                {
                    _completedSegments[i].Return();
                }

                _completedSegments.Clear();
            }

            if (_currentSegment != null)
            {
                ArrayPool<byte>.Shared.Return(_currentSegment);
                _currentSegment = null;
            }

            _bytesWritten = 0;
            _position = 0;
        }

        public void Advance(int count)
        {
            _bytesWritten += count;
            _position += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsMemory(_position, _currentSegment.Length - _position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);

            return _currentSegment.AsSpan(_position, _currentSegment.Length - _position);
        }

        public void CopyTo(IBufferWriter<byte> destination)
        {
            if (_completedSegments != null)
            {
                // Copy completed segments
                var count = _completedSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    destination.Write(_completedSegments[i].Span);
                }
            }

            destination.Write(_currentSegment.AsSpan(0, _position));
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (_completedSegments == null && _currentSegment is not null)
            {
                // There is only one segment so write without awaiting.
                return destination.WriteAsync(_currentSegment, 0, _position);
            }

            return CopyToSlowAsync(destination);
        }

        [MemberNotNull(nameof(_currentSegment))]
        private void EnsureCapacity(int sizeHint)
        {
            // This does the Right Thing. It only subtracts _position from the current segment length if it's non-null.
            // If _currentSegment is null, it returns 0.
            var remainingSize = _currentSegment?.Length - _position ?? 0;

            // If the sizeHint is 0, any capacity will do
            // Otherwise, the buffer must have enough space for the entire size hint, or we need to add a segment.
            if ((sizeHint == 0 && remainingSize > 0) || (sizeHint > 0 && remainingSize >= sizeHint))
            {
                // We have capacity in the current segment
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
                return;
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
            }

            AddSegment(sizeHint);
        }

        [MemberNotNull(nameof(_currentSegment))]
        private void AddSegment(int sizeHint = 0)
        {
            if (_currentSegment != null)
            {
                // We're adding a segment to the list
                if (_completedSegments == null)
                {
                    _completedSegments = new List<CompletedBuffer>();
                }

                // Position might be less than the segment length if there wasn't enough space to satisfy the sizeHint when
                // GetMemory was called. In that case we'll take the current segment and call it "completed", but need to
                // ignore any empty space in it.
                _completedSegments.Add(new CompletedBuffer(_currentSegment, _position));
            }

            // Get a new buffer using the minimum segment size, unless the size hint is larger than a single segment.
            _currentSegment = ArrayPool<byte>.Shared.Rent(Math.Max(_minimumSegmentSize, sizeHint));
            _position = 0;
        }

        private async Task CopyToSlowAsync(Stream destination)
        {
            if (_completedSegments != null)
            {
                // Copy full segments
                var count = _completedSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _completedSegments[i];
                    await destination.WriteAsync(segment.Buffer, 0, segment.Length);
                }
            }

            if (_currentSegment is not null)
            {
                await destination.WriteAsync(_currentSegment, 0, _position);
            }
        }

        public override void WriteByte(byte value)
        {
            if (_currentSegment != null && (uint)_position < (uint)_currentSegment.Length)
            {
                _currentSegment[_position] = value;
            }
            else
            {
                AddSegment();
                _currentSegment[0] = value;
            }

            _position++;
            _bytesWritten++;
        }

        // TODO inefficient, don't want to allocate array size of string,
        // but issues with decoder APIs returning character count larger than
        // required.
        public string GetString(Encoding? encoding)
        {
            if (_currentSegment == null || encoding == null)
            {
                return "";
            }

            var result = new byte[_bytesWritten];

            var totalWritten = 0;

            if (_completedSegments != null)
            {
                // Copy full segments
                var count = _completedSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _completedSegments[i];
                    segment.Span.CopyTo(result.AsSpan(totalWritten));
                    totalWritten += segment.Span.Length;
                }
            }

            // Copy current incomplete segment
            _currentSegment.AsSpan(0, _position).CopyTo(result.AsSpan(totalWritten));

            // If encoding is nullable
            return encoding.GetString(result);
        }

        public void CopyTo(Span<byte> span)
        {
            Debug.Assert(span.Length >= _bytesWritten);

            if (_currentSegment == null)
            {
                return;
            }

            var totalWritten = 0;

            if (_completedSegments != null)
            {
                // Copy full segments
                var count = _completedSegments.Count;
                for (var i = 0; i < count; i++)
                {
                    var segment = _completedSegments[i];
                    segment.Span.CopyTo(span.Slice(totalWritten));
                    totalWritten += segment.Span.Length;
                }
            }

            // Copy current incomplete segment
            _currentSegment.AsSpan(0, _position).CopyTo(span.Slice(totalWritten));

            Debug.Assert(_bytesWritten == totalWritten + _position);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }

        /// <summary>
        /// Holds a byte[] from the pool and a size value. Basically a Memory but guaranteed to be backed by an ArrayPool byte[], so that we know we can return it.
        /// </summary>
        private readonly struct CompletedBuffer
        {
            public byte[] Buffer { get; }
            public int Length { get; }

            public ReadOnlySpan<byte> Span => Buffer.AsSpan(0, Length);

            public CompletedBuffer(byte[] buffer, int length)
            {
                Buffer = buffer;
                Length = length;
            }

            public void Return()
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }
    }
}
