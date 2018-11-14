// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2OutputProducer : IHttpOutputProducer
    {
        private readonly int _streamId;
        private readonly IHttp2FrameWriter _frameWriter;

        public Http2OutputProducer(int streamId, IHttp2FrameWriter frameWriter)
        {
            _streamId = streamId;
            _frameWriter = frameWriter;
        }

        public void Dispose()
        {
        }

        public void Abort(ConnectionAbortedException error)
        {
            // TODO: RST_STREAM?
        }

        public Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state)
        {
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken) => _frameWriter.FlushAsync(cancellationToken);

        public Task Write100ContinueAsync(CancellationToken cancellationToken) => _frameWriter.Write100ContinueAsync(_streamId);

        public Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken)
        {
            return _frameWriter.WriteDataAsync(_streamId, data, cancellationToken);
        }

        public Task WriteStreamSuffixAsync(CancellationToken cancellationToken)
        {
            return _frameWriter.WriteDataAsync(_streamId, Constants.EmptyData, endStream: true, cancellationToken: cancellationToken);
        }

        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders)
        {
            _frameWriter.WriteResponseHeaders(_streamId, statusCode, responseHeaders);
        }
    }
}
