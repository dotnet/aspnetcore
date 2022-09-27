// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultWebSocketManager : WebSocketManager
{
    // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IHttpRequestFeature?> _nullRequestFeature = f => null;
    private static readonly Func<IFeatureCollection, IHttpWebSocketFeature?> _nullWebSocketFeature = f => null;

    private FeatureReferences<FeatureInterfaces> _features;
    private static readonly WebSocketAcceptContext _defaultWebSocketAcceptContext = new WebSocketAcceptContext();

    public DefaultWebSocketManager(IFeatureCollection features)
    {
        Initialize(features);
    }

    public void Initialize(IFeatureCollection features)
    {
        _features.Initalize(features);
    }

    public void Initialize(IFeatureCollection features, int revision)
    {
        _features.Initalize(features, revision);
    }

    public void Uninitialize()
    {
        _features = default;
    }

    private IHttpRequestFeature HttpRequestFeature =>
        _features.Fetch(ref _features.Cache.Request, _nullRequestFeature)!;

    private IHttpWebSocketFeature WebSocketFeature =>
        _features.Fetch(ref _features.Cache.WebSockets, _nullWebSocketFeature)!;

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
            return HttpRequestFeature.Headers.GetCommaSeparatedValues(HeaderNames.WebSocketSubProtocols);
        }
    }

    public override Task<WebSocket> AcceptWebSocketAsync(string? subProtocol)
    {
        var acceptContext = subProtocol is null ? _defaultWebSocketAcceptContext :
            new WebSocketAcceptContext() { SubProtocol = subProtocol };
        return AcceptWebSocketAsync(acceptContext);
    }

    public override Task<WebSocket> AcceptWebSocketAsync(WebSocketAcceptContext acceptContext)
    {
        if (WebSocketFeature == null)
        {
            throw new NotSupportedException("WebSockets are not supported");
        }
        return WebSocketFeature.AcceptAsync(acceptContext);
    }

    struct FeatureInterfaces
    {
        public IHttpRequestFeature? Request;
        public IHttpWebSocketFeature? WebSockets;
    }
}
