// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultWebSocketManager : WebSocketManager, IFeatureCache
    {
        private IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpRequestFeature _request;
        private IHttpWebSocketFeature _webSockets;

        public DefaultWebSocketManager(IFeatureCollection features)
        {
            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _request = null;
                _webSockets = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpRequestFeature HttpRequestFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _request); }
        }

        private IHttpWebSocketFeature WebSocketFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _webSockets); }
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
                return ParsingHelpers.GetHeaderSplit(HttpRequestFeature.Headers, HeaderNames.WebSocketSubProtocols);
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
