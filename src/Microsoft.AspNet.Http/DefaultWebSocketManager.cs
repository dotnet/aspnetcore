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
    public class DefaultWebSocketManager : WebSocketManager
    {
        private FeatureReferences<FeatureInterfaces> _features;

        public DefaultWebSocketManager(IFeatureCollection features)
        {
            Initialize(features);
        }

        public virtual void Initialize(IFeatureCollection features)
        {
            _features = new FeatureReferences<FeatureInterfaces>(features);
        }

        public virtual void Uninitialize()
        {
            _features = default(FeatureReferences<FeatureInterfaces>);
        }

        private IHttpRequestFeature HttpRequestFeature =>
            _features.Fetch(ref _features.Cache.Request, f => null);

        private IHttpWebSocketFeature WebSocketFeature =>
            _features.Fetch(ref _features.Cache.WebSockets, f => null);

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

        struct FeatureInterfaces
        {
            public IHttpRequestFeature Request;
            public IHttpWebSocketFeature WebSockets;
        }
    }
}
