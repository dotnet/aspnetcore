// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Stream wrapper that create specific compression stream only if necessary.
    /// </summary>
    internal class BodyWrapperStream : Stream, IHttpBufferingFeature, IHttpSendFileFeature
    {
        private readonly HttpResponse _response;
        private readonly Stream _bodyOriginalStream;
        private readonly IResponseCompressionProvider _provider;
        private readonly ICompressionProvider _compressionProvider;
        private readonly IHttpBufferingFeature _innerBufferFeature;
        private readonly IHttpSendFileFeature _innerSendFileFeature;

        private bool _compressionChecked = false;
        private Stream _compressionStream = null;

        internal BodyWrapperStream(HttpResponse response, Stream bodyOriginalStream, IResponseCompressionProvider provider, ICompressionProvider compressionProvider,
            IHttpBufferingFeature innerBufferFeature, IHttpSendFileFeature innerSendFileFeature)
        {
            _response = response;
            _bodyOriginalStream = bodyOriginalStream;
            _provider = provider;
            _compressionProvider = compressionProvider;
            _innerBufferFeature = innerBufferFeature;
            _innerSendFileFeature = innerSendFileFeature;
        }

        protected override void Dispose(bool disposing)
        {
            if (_compressionStream != null)
            {
                _compressionStream.Dispose();
                _compressionStream = null;
            }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _bodyOriginalStream.CanWrite;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            if (!_compressionChecked)
            {
                OnWrite();
                // Flush the original stream to send the headers. Flushing the compression stream won't
                // flush the original stream if no data has been written yet.
                _bodyOriginalStream.Flush();
                return;
            }

            if (_compressionStream != null)
            {
                _compressionStream.Flush();
            }
            else
            {
                _bodyOriginalStream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!_compressionChecked)
            {
                OnWrite();
                // Flush the original stream to send the headers. Flushing the compression stream won't
                // flush the original stream if no data has been written yet.
                return _bodyOriginalStream.FlushAsync(cancellationToken);
            }

            if (_compressionStream != null)
            {
                return _compressionStream.FlushAsync(cancellationToken);
            }

            return _bodyOriginalStream.FlushAsync(cancellationToken);
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
            }
            else
            {
                _bodyOriginalStream.Write(buffer, offset, count);
            }
        }

#if NET451
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                return _compressionStream.BeginWrite(buffer, offset, count, callback, state);
            }
            return _bodyOriginalStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (!_compressionChecked)
            {
                throw new InvalidOperationException("BeginWrite was not called before EndWrite");
            }

            if (_compressionStream != null)
            {
                _compressionStream.EndWrite(asyncResult);
            }
            else
            {
                _bodyOriginalStream.EndWrite(asyncResult);
            }
        }
#endif

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                return _compressionStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            return _bodyOriginalStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        private void OnWrite()
        {
            if (!_compressionChecked)
            {
                _compressionChecked = true;

                if (IsCompressable())
                {
                    _response.Headers.Append(HeaderNames.ContentEncoding, _compressionProvider.EncodingName);
                    _response.Headers.Remove(HeaderNames.ContentMD5); // Reset the MD5 because the content changed.
                    _response.Headers.Remove(HeaderNames.ContentLength);

                    _compressionStream = _compressionProvider.CreateStream(_bodyOriginalStream);
                }
            }
        }

        private bool IsCompressable()
        {
            return !_response.Headers.ContainsKey(HeaderNames.ContentRange) &&     // The response is not partial
                _provider.ShouldCompressResponse(_response.HttpContext);
        }

        public void DisableRequestBuffering()
        {
            // Unrelated
            _innerBufferFeature?.DisableRequestBuffering();
        }

        // For this to be effective it needs to be called before the first write.
        public void DisableResponseBuffering()
        {
            if (!_compressionProvider.SupportsFlush)
            {
                // Don't compress, some of the providers don't implement Flush (e.g. .NET 4.5.1 GZip/Deflate stream)
                // which would block real-time responses like SignalR.
                _compressionChecked = true;
            }

            _innerBufferFeature?.DisableResponseBuffering();
        }

        // The IHttpSendFileFeature feature will only be registered if _innerSendFileFeature exists.
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            OnWrite();

            if (_compressionStream != null)
            {
                return InnerSendFileAsync(path, offset, count, cancellation);
            }

            return _innerSendFileFeature.SendFileAsync(path, offset, count, cancellation);
        }

        private async Task InnerSendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(path);
            if (offset < 0 || offset > fileInfo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }
            if (count.HasValue &&
                (count.Value < 0 || count.Value > fileInfo.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }

            int bufferSize = 1024 * 16;

            var fileStream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: bufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await StreamCopyOperation.CopyToAsync(fileStream, _compressionStream, count, cancellation);
            }
        }
    }
}
