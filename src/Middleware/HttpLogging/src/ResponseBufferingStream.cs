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
using static Microsoft.AspNetCore.HttpLogging.MediaTypeOptions;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Stream that buffers reads 
    /// </summary>
    internal sealed class ResponseBufferingStream : BufferingStream, IHttpResponseBodyFeature
    {
        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly int _limit;
        private PipeWriter? _pipeAdapter;

        private readonly HttpContext _context;
        private readonly List<MediaTypeState> _encodings;
        private readonly HttpLoggingOptions _options;
        private Encoding? _encoding;

        private static readonly StreamPipeWriterOptions _pipeWriterOptions = new StreamPipeWriterOptions(leaveOpen: true);

        internal ResponseBufferingStream(IHttpResponseBodyFeature innerBodyFeature,
            int limit,
            ILogger logger,
            HttpContext context,
            List<MediaTypeState> encodings,
            HttpLoggingOptions options)
            : base(innerBodyFeature.Stream, logger)
        {
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
            _limit = limit;
            _context = context;
            _encodings = encodings;
            _options = options;
        }

        public bool FirstWrite { get; private set; }

        public Stream Stream => this;

        public PipeWriter Writer => _pipeAdapter ??= PipeWriter.Create(Stream, _pipeWriterOptions);

        public Encoding? Encoding { get => _encoding; }

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
            var remaining = _limit - _bytesBuffered;
            var innerCount = Math.Min(remaining, span.Length);

            OnFirstWrite().AsTask().GetAwaiter().GetResult();

            if (innerCount > 0)
            {    
                if (span.Slice(0, innerCount).TryCopyTo(_tailMemory.Span))
                {
                    _tailBytesBuffered += innerCount;
                    _bytesBuffered += innerCount;
                    _tailMemory = _tailMemory.Slice(innerCount);
                }
                else
                {
                    BuffersExtensions.Write(this, span.Slice(0, innerCount));
                }
            }

            _innerStream.Write(span);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var remaining = _limit - _bytesBuffered;
            var innerCount = Math.Min(remaining, buffer.Length);

            await OnFirstWrite();

            if (innerCount > 0)
            {
                if (_tailMemory.Length - innerCount > 0)
                {
                    buffer.Slice(0, innerCount).CopyTo(_tailMemory);
                    _tailBytesBuffered += innerCount;
                    _bytesBuffered += innerCount;
                    _tailMemory = _tailMemory.Slice(innerCount);
                }
                else
                {
                    BuffersExtensions.Write(this, buffer.Span);
                }
            }

            await _innerStream.WriteAsync(buffer, cancellationToken);
        }

        private async ValueTask OnFirstWrite()
        {
            if (!FirstWrite)
            {
                // Log headers as first write occurs (headers locked now)
                await HttpLoggingMiddleware.LogResponseHeaders(_context, _options, _logger);

                MediaTypeHelpers.TryGetEncodingForMediaType(_context.Response.ContentType, _encodings, out _encoding);
                FirstWrite = true;
            }
        }

        public void DisableBuffering()
        {
            _innerBodyFeature.DisableBuffering();
        }

        public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            await OnFirstWrite();
            await _innerBodyFeature.SendFileAsync(path, offset, count, cancellation);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            await OnFirstWrite();
            await _innerBodyFeature.StartAsync(token);
        }

        public async Task CompleteAsync()
        {
            await _innerBodyFeature.CompleteAsync();
        }

        public override void Flush()
        {
            OnFirstWrite().AsTask().GetAwaiter().GetResult();
            base.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await OnFirstWrite();
            await base.FlushAsync(cancellationToken);
        }
    }
}
