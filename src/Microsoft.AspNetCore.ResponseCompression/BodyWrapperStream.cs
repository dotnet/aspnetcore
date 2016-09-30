// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCompression
{
    /// <summary>
    /// Stream wrapper that create specific compression stream only if necessary.
    /// </summary>
    internal class BodyWrapperStream : Stream
    {
        private readonly HttpResponse _response;

        private readonly Stream _bodyOriginalStream;

        private readonly IResponseCompressionProvider _provider;

        private readonly ICompressionProvider _compressionProvider;

        private bool _compressionChecked = false;

        private Stream _compressionStream = null;

        internal BodyWrapperStream(HttpResponse response, Stream bodyOriginalStream, IResponseCompressionProvider provider, ICompressionProvider compressionProvider)
        {
            _response = response;
            _bodyOriginalStream = bodyOriginalStream;
            _provider = provider;
            _compressionProvider = compressionProvider;
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
            OnWrite();

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
            OnWrite();

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
            OnWrite();

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
    }
}
