// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal class Http2Streams
    {
        private readonly FrameResponseStream _response;
        private readonly FrameRequestStream _request;

        public Http2Streams(IHttpBodyControlFeature bodyControl, IFrameControl httpStreamControl)
        {
            _request = new FrameRequestStream(bodyControl);
            _response = new FrameResponseStream(bodyControl, httpStreamControl);
        }

        public (Stream request, Stream response) Start(Http2MessageBody body)
        {
            _request.StartAcceptingReads(body);
            _response.StartAcceptingWrites();

            return (_request, _response);
        }

        public void Pause()
        {
            _request.PauseAcceptingReads();
            _response.PauseAcceptingWrites();
        }

        public void Stop()
        {
            _request.StopAcceptingReads();
            _response.StopAcceptingWrites();
        }

        public void Abort(Exception error)
        {
            _request.Abort(error);
            _response.Abort();
        }
    }
}
