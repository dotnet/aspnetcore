// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class Streams
    {
        private static readonly ThrowingWasUpgradedWriteOnlyStream _throwingResponseStream
            = new ThrowingWasUpgradedWriteOnlyStream();
        private readonly HttpResponseStream _response;
        private readonly HttpRequestStream _request;
        private readonly WrappingStream _upgradeableResponse;
        private readonly HttpRequestStream _emptyRequest;
        private readonly Stream _upgradeStream;

        public Streams(IHttpBodyControlFeature bodyControl, IHttpResponseControl httpResponseControl)
        {
            _request = new HttpRequestStream(bodyControl);
            _emptyRequest = new HttpRequestStream(bodyControl);
            _response = new HttpResponseStream(bodyControl, httpResponseControl);
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

        public (Stream request, Stream response) Start(MessageBody body)
        {
            _request.StartAcceptingReads(body);
            _emptyRequest.StartAcceptingReads(MessageBody.ZeroContentLengthClose);
            _response.StartAcceptingWrites();

            if (body.RequestUpgrade)
            {
                // until Upgrade() is called, context.Response.Body should use the normal output stream
                _upgradeableResponse.SetInnerStream(_response);
                // upgradeable requests should never have a request body
                return (_emptyRequest, _upgradeableResponse);
            }
            else
            {
                return (_request, _response);
            }
        }

        public void Stop()
        {
            _request.StopAcceptingReads();
            _emptyRequest.StopAcceptingReads();
            _response.StopAcceptingWrites();
        }

        public void Abort(Exception error)
        {
            _request.Abort(error);
            _emptyRequest.Abort(error);
            _response.Abort();
        }
    }
}
