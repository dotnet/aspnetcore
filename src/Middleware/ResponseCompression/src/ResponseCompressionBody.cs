// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Stream wrapper that create specific compression stream only if necessary.
    /// </summary>
    internal class ResponseCompressionBody : Stream, IHttpResponseBodyFeature, IHttpsCompressionFeature
    {
        private readonly HttpContext _context;
        private readonly IResponseCompressionProvider _provider;
        private readonly IHttpResponseBodyFeature _innerBodyFeature;
        private readonly Stream _innerStream;

        private ICompressionProvider? _compressionProvider;
        private bool _compressionChecked;
        private Stream? _compressionStream;
        private PipeWriter? _pipeAdapter;
        private bool _providerCreated;
        private bool _autoFlush;
        private bool _complete;

        internal ResponseCompressionBody(HttpContext context, IResponseCompressionProvider provider,
            IHttpResponseBodyFeature innerBodyFeature)
        {
            _context = context;
            _provider = provider;
            _innerBodyFeature = innerBodyFeature;
            _innerStream = innerBodyFeature.Stream;
        }

        internal async Task FinishCompressionAsync()
        {
            if (_complete)
            {
                return;
            }

            _complete = true;

            if (_pipeAdapter != null)
            {
                await _pipeAdapter.CompleteAsync();
            }

            if (_compressionStream != null)
            {
                await _compressionStream.DisposeAsync();
            }

            // Adds the compression headers for HEAD requests even if the body was not used.
            if (!_compressionChecked && HttpMethods.IsHead(_context.Request.Method))
            {
                InitializeCompressionHeaders();
            }
        }

        HttpsCompressionMode IHttpsCompressionFeature.Mode { get; set; } = HttpsCompressionMode.Default;

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

        public override void Flush()
        {
            if (!_compressionChecked)
            {
                OnWrite();
                // Flush the original stream to send the headers. Flushing the compression stream won't
                // flush the original stream if no data has been written yet.
                _innerStream.Flush();
                return;
            }

            if (_compressionStream != null)
            {
                _compressionStream.Flush();
            }
            else
            {
                _innerStream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!_compressionChecked)
            {
                OnWrite();
                // Flush the original stream to send the headers. Flushing the compression stream won't
                // flush the original stream if no data has been written yet.
                return _innerStream.FlushAsync(cancellationToken);
            }

            if (_compressionStream != null)
            {
                return _compressionStream.FlushAsync(cancellationToken);
            }

            return _innerStream.FlushAsync(cancellationToken);
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                _compressionStream.Write(buffer, offset, count);
                if (_autoFlush)
                {
                    _compressionStream.Flush();
                }
            }
            else
            {
                _innerStream.Write(buffer, offset, count);
            }
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

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                await _compressionStream.WriteAsync(buffer, offset, count, cancellationToken);
                if (_autoFlush)
                {
                    await _compressionStream.FlushAsync(cancellationToken);
                }
            }
            else
            {
                await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        private void InitializeCompressionHeaders()
        {
            if (_provider.ShouldCompressResponse(_context))
            {
                // If the MIME type indicates that the response could be compressed, caches will need to vary by the Accept-Encoding header
                var varyValues = _context.Response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
                var varyByAcceptEncoding = false;

                for (var i = 0; i < varyValues.Length; i++)
                {
                    if (string.Equals(varyValues[i], HeaderNames.AcceptEncoding, StringComparison.OrdinalIgnoreCase))
                    {
                        varyByAcceptEncoding = true;
                        break;
                    }
                }

                if (!varyByAcceptEncoding)
                {
                    _context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);
                }

                var compressionProvider = ResolveCompressionProvider();
                if (compressionProvider != null)
                {
                    _context.Response.Headers.Append(HeaderNames.ContentEncoding, compressionProvider.EncodingName);
                    _context.Response.Headers.Remove(HeaderNames.ContentMD5); // Reset the MD5 because the content changed.
                    _context.Response.Headers.Remove(HeaderNames.ContentLength);
                }
            }
        }

        private void OnWrite()
        {
            if (!_compressionChecked)
            {
                _compressionChecked = true;

                InitializeCompressionHeaders();

                if (_compressionProvider != null)
                {
                    _compressionStream = _compressionProvider.CreateStream(_innerStream);
                }
            }
        }

        private ICompressionProvider? ResolveCompressionProvider()
        {
            if (!_providerCreated)
            {
                _providerCreated = true;
                _compressionProvider = _provider.GetCompressionProvider(_context);
            }

            return _compressionProvider;
        }

        // For this to be effective it needs to be called before the first write.
        public void DisableBuffering()
        {
            if (ResolveCompressionProvider()?.SupportsFlush == false)
            {
                // Don't compress, some of the providers don't implement Flush (e.g. .NET 4.5.1 GZip/Deflate stream)
                // which would block real-time responses like SignalR.
                _compressionChecked = true;
            }
            else
            {
                _autoFlush = true;
            }
            _innerBodyFeature.DisableBuffering();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                return SendFileFallback.SendFileAsync(Stream, path, offset, count, cancellation);
            }

            return _innerBodyFeature.SendFileAsync(path, offset, count, cancellation);
        }

        public Task StartAsync(CancellationToken token = default)
        {
            OnWrite();
            return _innerBodyFeature.StartAsync(token);
        }

        public async Task CompleteAsync()
        {
            if (_complete)
            {
                return;
            }

            await FinishCompressionAsync(); // Sets _complete
            await _innerBodyFeature.CompleteAsync();
        }
    }
}
