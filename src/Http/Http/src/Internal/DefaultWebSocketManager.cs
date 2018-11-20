// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class DefaultWebSocketManager : WebSocketManager
    {
        // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IHttpRequestFeature> _nullRequestFeature = f => null;
        private readonly static Func<IFeatureCollection, IHttpWebSocketFeature> _nullWebSocketFeature = f => null;

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
            _features.Fetch(ref _features.Cache.Request, _nullRequestFeature);

        private IHttpWebSocketFeature WebSocketFeature =>
            _features.Fetch(ref _features.Cache.WebSockets, _nullWebSocketFeature);

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
