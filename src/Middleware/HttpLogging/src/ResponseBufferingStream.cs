// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Stream that buffers reads 
    /// </summary>
    internal class ResponseBufferingStream : Stream, IHttpResponseBodyFeature, IBufferWriter<byte>
    {
        private const int MinimumBufferSize = 4096; // 4K

        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;
        private readonly int _limit;
        private PipeWriter? _pipeAdapter;

        private int _bytesWritten;

        private readonly ILogger _logger;
        private readonly HttpContext _context;
        private readonly List<KeyValuePair<MediaTypeHeaderValue, Encoding>> _encodings;
        private BufferSegment? _head;
        private BufferSegment? _tail;
        private Memory<byte> _tailMemory; // remainder of tail memory
        private int _tailBytesBuffered;

        private Encoding? _encoding;
        private bool _hasCheckedEncoding;

        private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

        internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
            int limit, ILogger logger,
            HttpContext context,
            List<KeyValuePair<MediaTypeHeaderValue, Encoding>> encodings)
        {
            // TODO need first write event
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _limit = limit;
            _logger = logger;
            _context = context;
            _encodings = encodings;
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

        public void Reset()
        {
            var segment = _head;
            while (segment != null)
            {
                var returnSegment = segment;
                segment = segment.NextSegment;

                // We haven't reached the tail of the linked list yet, so we can always return the returnSegment.
                returnSegment.ResetMemory();
            }

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
            if (!_hasCheckedEncoding)
            {
                MediaTypeHelpers.TryGetEncodingForMediaType(_context.Response.ContentType, _encodings, out _encoding);
                _hasCheckedEncoding = true;
                // TODO can short circuit this and not allocate anything if encoding is null after this.
            }

            BufferSegment newSegment = CreateSegmentUnsynchronized();

            // We can't use the recommended pool so use the ArrayPool
            newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(GetSegmentSize(sizeHint)));

            _tailMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private BufferSegment CreateSegmentUnsynchronized()
        {
            return new BufferSegment();
        }

        private static int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(MinimumBufferSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            var adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }

        public void LogString()
        {
            if (_head == null || _tail == null || _encoding == null)
            {
                return;
            }

            // Only place where we are actually using the buffered data.
            // update tail here.
            _tail.End = _tailBytesBuffered;

            var ros = new ReadOnlySequence<byte>(_head, 0, _tail, _tailBytesBuffered);

            var body = _encoding.GetString(ros);

            _logger.LogInformation(LoggerEventIds.ResponseBody, CoreStrings.ResponseBody, body);

            // Don't hold onto buffers anymore
            Reset();
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
