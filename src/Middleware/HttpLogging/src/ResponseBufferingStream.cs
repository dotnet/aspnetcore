// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class ResponseBufferingStream : Stream, IHttpResponseBodyFeature, IBufferWriter<byte>
    {
        private const int MaxSegmentPoolSize = 256; // 1MB
        private const int MinimumBufferSize = 4096; // 4K

        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;
        private readonly int _limit;
        private PipeWriter? _pipeAdapter;

        private int _bytesWritten;

        private readonly BufferSegmentStack _bufferSegmentPool;
        private BufferSegment? _head;
        private BufferSegment? _tail;
        private Memory<byte> _tailMemory; // remainder of tail memory
        private int _tailBytesBuffered;

        private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

        internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature, int limit)
        {
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _limit = limit;
            _bufferSegmentPool = new BufferSegmentStack(limit / MinimumBufferSize);
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
                    _pipeAdapter = PipeWriter.Create(Stream, _pipeWriterOptions);
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
            Write(buffer.AsSpan(offset, count));
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToApm.End(asyncResult);
        }

        public override void Write(ReadOnlySpan<byte> span)
        {
            var remaining = _limit - _bytesWritten;
            var innerCount = Math.Min(remaining, span.Length);

            if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
            {
                _tailBytesBuffered += innerCount;
                _bytesWritten += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
            }
            else
            {
                BuffersExtensions.Write(this, span.Slice(0, innerCount));
            }

            _innerStream.Write(span);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var remaining = _limit - _bytesWritten;
            var innerCount = Math.Min(remaining, count);

            if (_tailMemory.Length - innerCount > 0)
            {
                //Buffer.BlockCopy(buffer, offset, , position, innerCount);
                buffer.AsSpan(offset, count).CopyTo(_tailMemory.Span);
                _tailBytesBuffered += innerCount;
                _bytesWritten += innerCount;
                _tailMemory = _tailMemory.Slice(innerCount);
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
            _bytesWritten = 0;
            _tailBytesBuffered = 0;
        }

        public void Advance(int bytes)
        {
            if ((uint)bytes > (uint)_tailMemory.Length)
            {
                ThrowArgumentOutOfRangeException(nameof(bytes));
            }

            _tailBytesBuffered += bytes;
            _bytesWritten += bytes;
            _tailMemory = _tailMemory.Slice(bytes);
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            AllocateMemoryUnsynchronized(sizeHint);
            return _tailMemory.Span;
        }

        private void AllocateMemoryUnsynchronized(int sizeHint)
        {
            if (_head == null)
            {
                // We need to allocate memory to write since nobody has written before
                BufferSegment newSegment = AllocateSegmentUnsynchronized(sizeHint);

                // Set all the pointers
                _head = _tail = newSegment;
                _tailBytesBuffered = 0;
            }
            else
            {
                int bytesLeftInBuffer = _tailMemory.Length;

                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
                {
                    Debug.Assert(_tail != null);

                    if (_tailBytesBuffered > 0)
                    {
                        // Flush buffered data to the segment
                        _tail.End += _tailBytesBuffered;
                        _tailBytesBuffered = 0;
                    }

                    BufferSegment newSegment = AllocateSegmentUnsynchronized(sizeHint);

                    _tail.SetNext(newSegment);
                    _tail = newSegment;
                }
            }
        }

        private BufferSegment AllocateSegmentUnsynchronized(int sizeHint)
        {
            BufferSegment newSegment = CreateSegmentUnsynchronized();

            // We can't use the recommended pool so use the ArrayPool
            newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(Math.Max(MinimumBufferSize, sizeHint)));

            _tailMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (_bufferSegmentPool.TryPop(out var segment))
            {
                return segment;
            }

            return new BufferSegment();
        }

        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            if (_bufferSegmentPool.Count < MaxSegmentPoolSize)
            {
                _bufferSegmentPool.Push(segment);
            }
        }

        private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(MinimumBufferSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            if (_bytesWritten == _limit)
            {
                return;
            }

            if (_tailMemory.Length == 0)
            {
                AllocateMemoryUnsynchronized(MinimumBufferSize);
            }

            _tailMemory.Span[0] = value;
            _tailBytesBuffered++;
            _bytesWritten++;
            _tailMemory = _tailMemory.Slice(1);
        }

        // TODO inefficient, don't want to allocate array size of string,
        // but issues with decoder APIs returning character count larger than
        // required.
        public string GetString(Encoding? encoding)
        {
            if (_head == null || _tail == null || encoding == null)
            {
                return "";
            }

            // Only place where we are actually using the buffered data.
            // update tail here.
            _tail.End = _tailBytesBuffered;

            var ros = new ReadOnlySequence<byte>(_head, 0, _tail, _tailBytesBuffered);

            // If encoding is nullable
            return encoding.GetString(ros);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }

        // Copied from https://github.com/dotnet/corefx/blob/de3902bb56f1254ec1af4bf7d092fc2c048734cc/src/System.Memory/src/System/ThrowHelper.cs
        private static void ThrowArgumentOutOfRangeException(string argumentName) { throw CreateArgumentOutOfRangeException(argumentName); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateArgumentOutOfRangeException(string argumentName) { return new ArgumentOutOfRangeException(argumentName); }
    }
}
