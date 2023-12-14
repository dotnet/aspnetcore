// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CodeGenerator;

public class HttpProtocolFeatureCollection
{
    public static string GenerateFile()
    {
        var alwaysFeatures = new[]
        {
            "IHttpRequestFeature",
            "IHttpResponseFeature",
            "IHttpResponseBodyFeature",
            "IRouteValuesFeature",
            "IEndpointFeature",
            "IServiceProvidersFeature",
        };

        var commonFeatures = new[]
        {
            "IHttpActivityFeature",
            "IHttpMetricsTagsFeature",
            "IItemsFeature",
            "IQueryFeature",
            "IRequestBodyPipeFeature",
            "IFormFeature",
            "IHttpAuthenticationFeature",
            "IHttpRequestIdentifierFeature",
        };

        var sometimesFeatures = new[]
        {
            "IHttpConnectionFeature",
            "ISessionFeature",
            "IResponseCookiesFeature",
            "IHttpRequestTrailersFeature",
            "IHttpResponseTrailersFeature",
            "ITlsConnectionFeature",
            "IHttpExtendedConnectFeature",
            "IHttpUpgradeFeature",
            "IHttpWebSocketFeature",
            "IHttpWebTransportFeature",
            "IBadRequestExceptionFeature",
            "IHttpRequestTimeoutFeature"
        };
        var maybeFeatures = new[]
        {
            "IHttp2StreamIdFeature",
            "IHttpRequestLifetimeFeature",
            "IHttpMaxRequestBodySizeFeature",
            "IHttpMinRequestBodyDataRateFeature",
            "IHttpMinResponseDataRateFeature",
            "IHttpBodyControlFeature",
            "IHttpRequestBodyDetectionFeature",
            "IHttpResetFeature",
            "IPersistentStateFeature"
        };

        var allFeatures = alwaysFeatures
            .Concat(commonFeatures)
            .Concat(sometimesFeatures)
            .Concat(maybeFeatures)
            .ToArray();

        // NOTE: This list MUST always match the set of feature interfaces implemented by HttpProtocol.
        // See also: src/Kestrel.Core/Internal/Http/HttpProtocol.FeatureCollection.cs
        var implementedFeatures = new[]
        {
            "IHttpRequestFeature",
            "IHttpResponseFeature",
            "IHttpResponseBodyFeature",
            "IRouteValuesFeature",
            "IEndpointFeature",
            "IHttpRequestIdentifierFeature",
            "IHttpRequestTrailersFeature",
            "IHttpExtendedConnectFeature",
            "IHttpUpgradeFeature",
            "IRequestBodyPipeFeature",
            "IHttpConnectionFeature",
            "IHttpRequestLifetimeFeature",
            "IHttpBodyControlFeature",
            "IHttpMaxRequestBodySizeFeature",
            "IHttpRequestBodyDetectionFeature",
            "IHttpWebTransportFeature",
            "IBadRequestExceptionFeature"
        };

        var usings = $@"
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

#pragma warning disable CA2252 // WebTransport is a preview feature";

        return FeatureCollectionGenerator.GenerateFile(
            namespaceName: "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http",
            className: "HttpProtocol",
            allFeatures: allFeatures,
            implementedFeatures: implementedFeatures,
            extraUsings: usings,
            fallbackFeatures: "ConnectionFeatures");
    }
}
