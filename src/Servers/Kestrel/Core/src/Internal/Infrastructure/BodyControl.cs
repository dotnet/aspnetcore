// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class BodyControl
    {
        private static readonly ThrowingWasUpgradedWriteOnlyStream _throwingResponseStream
            = new ThrowingWasUpgradedWriteOnlyStream();
        private readonly HttpResponseStream _response;
        private readonly HttpResponsePipeWriter _responseWriter;
        private readonly HttpRequestPipeReader _requestReader;
        private readonly HttpRequestStream _request;
        private readonly HttpRequestPipeReader _emptyRequestReader;
        private readonly WrappingStream _upgradeableResponse;
        private readonly HttpRequestStream _emptyRequest;
        private readonly Stream _upgradeStream;

        public BodyControl(IHttpBodyControlFeature bodyControl, IHttpResponseControl responseControl)
        {
            _requestReader = new HttpRequestPipeReader();
            _request = new HttpRequestStream(bodyControl, _requestReader);
            _emptyRequestReader = new HttpRequestPipeReader();
            _emptyRequest = new HttpRequestStream(bodyControl, _emptyRequestReader);

            _responseWriter = new HttpResponsePipeWriter(responseControl);
            _response = new HttpResponseStream(bodyControl, _responseWriter);
            _upgradeableResponse = new WrappingStream(_response);
            _upgradeStream = new HttpUpgradeStream(_request, _response);
        }

        public Stream Upgrade()
        {
            // causes writes to context.Response.Body to throw
            _upgradeableResponse.SetInnerStream(_throwingResponseStream);
            // _upgradeStream always uses _response
            return _upgradeStream;
        }

        public (Stream request, Stream response, PipeReader reader, PipeWriter writer) Start(MessageBody body)
        {
            _requestReader.StartAcceptingReads(body);
            _emptyRequestReader.StartAcceptingReads(MessageBody.ZeroContentLengthClose);
            _responseWriter.StartAcceptingWrites();

            if (body.RequestUpgrade)
            {
                // until Upgrade() is called, context.Response.Body should use the normal output stream
                _upgradeableResponse.SetInnerStream(_response);
                // upgradeable requests should never have a request body
                return (_emptyRequest, _upgradeableResponse, _emptyRequestReader, _responseWriter);
            }
            else
            {
                return (_request, _response, _requestReader, _responseWriter);
            }
        }

        public Task StopAsync()
        {
            _requestReader.StopAcceptingReads();
            _emptyRequestReader.StopAcceptingReads();
            return _responseWriter.StopAcceptingWritesAsync();
        }

        public void Abort(Exception error)
        {
            _requestReader.Abort(error);
            _emptyRequestReader.Abort(error);
            _responseWriter.Abort();
        }
    }
}
