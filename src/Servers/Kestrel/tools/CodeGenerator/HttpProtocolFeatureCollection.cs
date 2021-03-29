// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace CodeGenerator
{
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
                "IServiceProvidersFeature"
            };

            var commonFeatures = new[]
            {
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
                "IHttpUpgradeFeature",
                "IHttpWebSocketFeature"
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
                "IHttpResetFeature"
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
                "IHttpUpgradeFeature",
                "IRequestBodyPipeFeature",
                "IHttpConnectionFeature",
                "IHttpRequestLifetimeFeature",
                "IHttpBodyControlFeature",
                "IHttpMaxRequestBodySizeFeature",
                "IHttpMinRequestBodyDataRateFeature",
                "IHttpRequestBodyDetectionFeature",
            };

            // NOTE: Each item in this list MUST always be reset by each protocol in their OnReset() method
            var skipResetFeatures = new[]
            {
                "IHttpMinRequestBodyDataRateFeature"
            };

            var usings = $@"
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;";

            return FeatureCollectionGenerator.GenerateFile(
                namespaceName: "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http",
                className: "HttpProtocol",
                allFeatures: allFeatures,
                implementedFeatures: implementedFeatures,
                skipResetFeatures: skipResetFeatures,
                extraUsings: usings,
                fallbackFeatures: "ConnectionFeatures");
        }
    }
}
