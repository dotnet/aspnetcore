// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Based on https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/blob/128b5f524e86954e997f2c453e7e5c1dcc3db746/Westwind.AspnetCore.LiveReload/ResponseStreamWrapper.cs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// Wraps the Response Stream to inject the WebSocket HTML into
    /// an HTML Page.
    /// </summary>
    public class ResponseStreamWrapper : Stream
    {
        private static readonly MediaTypeHeaderValue _textHtmlMediaType = new MediaTypeHeaderValue("text/html");
        private readonly Stream _baseStream;
        private readonly HttpContext _context;
        private readonly ILogger _logger;
        private bool? _isHtmlResponse;

        public ResponseStreamWrapper(HttpContext context, ILogger logger)
        {
            _context = context;
            _baseStream = context.Response.Body;
            _logger = logger;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length { get; }
        public override long Position { get; set; }
        public bool ScriptInjectionPerformed { get; private set; }

        public bool IsHtmlResponse => _isHtmlResponse ?? false;

        public override void Flush()
        {
            OnWrite();
            _baseStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            OnWrite();
            return _baseStream.FlushAsync(cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            OnWrite();
            if (IsHtmlResponse && !ScriptInjectionPerformed)
            {
                ScriptInjectionPerformed = WebSocketScriptInjection.TryInjectLiveReloadScript(_baseStream, buffer);
            }
            else
            {
                _baseStream.Write(buffer);
            }
        }

        public override void WriteByte(byte value)
        {
            OnWrite();
            _baseStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            OnWrite();

            if (IsHtmlResponse && !ScriptInjectionPerformed)
            {
                ScriptInjectionPerformed = WebSocketScriptInjection.TryInjectLiveReloadScript(_baseStream, buffer.AsSpan(offset, count));
            }
            else
            {
                _baseStream.Write(buffer, offset, count);
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnWrite();

            if (IsHtmlResponse && !ScriptInjectionPerformed)
            {
                ScriptInjectionPerformed = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(_baseStream, buffer.AsMemory(offset, count), cancellationToken);
            }
            else
            {
                await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            OnWrite();

            if (IsHtmlResponse && !ScriptInjectionPerformed)
            {
                ScriptInjectionPerformed = await WebSocketScriptInjection.TryInjectLiveReloadScriptAsync(_baseStream, buffer, cancellationToken);
            }
            else
            {
                await _baseStream.WriteAsync(buffer, cancellationToken);
            }
        }

        private void OnWrite()
        {
            if (_isHtmlResponse.HasValue)
            {
                return;
            }

            var response = _context.Response;

            _isHtmlResponse =
                (response.StatusCode == StatusCodes.Status200OK || response.StatusCode == StatusCodes.Status500InternalServerError) &&
                MediaTypeHeaderValue.TryParse(response.ContentType, out var mediaType) &&
                mediaType.IsSubsetOf(_textHtmlMediaType) &&
                (!mediaType.Charset.HasValue || mediaType.Charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase));

            if (_isHtmlResponse.Value)
            {
                BrowserRefreshMiddleware.Log.SetupResponseForBrowserRefresh(_logger);

                // Since we're changing the markup content, reset the content-length
                response.Headers.ContentLength = null;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
             => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
             => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
