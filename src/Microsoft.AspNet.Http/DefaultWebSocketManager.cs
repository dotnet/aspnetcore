// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http
{
    public class DefaultWebSocketManager : WebSocketManager
    {
        private static IList<string> EmptyList = new List<string>();

        private IFeatureCollection _features;
        private FeatureReference<IHttpRequestFeature> _request = FeatureReference<IHttpRequestFeature>.Default;
        private FeatureReference<IHttpWebSocketFeature> _webSockets = FeatureReference<IHttpWebSocketFeature>.Default;

        public DefaultWebSocketManager(IFeatureCollection features)
        {
            _features = features;
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return _request.Fetch(_features); }
        }

        private IHttpWebSocketFeature WebSocketFeature
        {
            get { return _webSockets.Fetch(_features); }
        }

        public override bool IsWebSocketRequest
        {
            get
            {
                return WebSocketFeature != null && WebSocketFeature.IsWebSocketRequest;
            }
        }

        public override IList<string> WebSocketRequestedProtocols
        {
            get
            {
                return ParsingHelpers.GetHeaderUnmodified(HttpRequestFeature.Headers,
                    HeaderNames.WebSocketSubProtocols) ?? EmptyList;
            }
        }

        public override Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
        {
            if (WebSocketFeature == null)
            {
                throw new NotSupportedException("WebSockets are not supported");
            }
            return WebSocketFeature.AcceptAsync(new WebSocketAcceptContext() { SubProtocol = subProtocol });
        }
    }
}
