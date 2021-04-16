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
    internal class ResponseBufferingStream : BufferingStream, IHttpResponseBodyFeature
    {
        private const int MinimumBufferSize = 4096; // 4K

        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;
        private readonly int _limit;
        private PipeWriter? _pipeAdapter;

        private int _bytesWritten;

        private readonly HttpContext _context;
        private readonly List<KeyValuePair<MediaTypeHeaderValue, Encoding>> _encodings;

        private Encoding? _encoding;
        private bool _hasCheckedEncoding;

        private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

        internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
            int limit, ILogger logger,
            HttpContext context,
            List<KeyValuePair<MediaTypeHeaderValue, Encoding>> encodings)
            : base (logger)
        {
            // TODO need first write event
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _limit = limit;
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

        public Encoding? Encoding { get => _encoding; }

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

            OnFirstWrite();

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

            OnFirstWrite();

            if (_tailMemory.Length - innerCount > 0)
            {
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

        private void OnFirstWrite()
        {
            if (!_hasCheckedEncoding)
            {
                MediaTypeHelpers.TryGetEncodingForMediaType(_context.Response.ContentType, _encodings, out _encoding);
                _hasCheckedEncoding = true;
            }
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
    }
}
