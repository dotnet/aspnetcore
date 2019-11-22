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
                "IRequestBodyPipeFeature",
                "IHttpRequestIdentifierFeature",
                "IServiceProvidersFeature",
                "IHttpRequestLifetimeFeature",
                "IHttpConnectionFeature",
                "IRouteValuesFeature",
                "IEndpointFeature"
            };

            var commonFeatures = new[]
            {
                "IHttpAuthenticationFeature",
                "IHttpRequestTrailersFeature",
                "IQueryFeature",
                "IFormFeature",
            };

            var sometimesFeatures = new[]
            {
                "IHttpUpgradeFeature",
                "IHttp2StreamIdFeature",
                "IHttpResponseTrailersFeature",
                "IResponseCookiesFeature",
                "IItemsFeature",
                "ITlsConnectionFeature",
                "IHttpWebSocketFeature",
                "ISessionFeature",
                "IHttpMaxRequestBodySizeFeature",
                "IHttpMinRequestBodyDataRateFeature",
                "IHttpMinResponseDataRateFeature",
                "IHttpBodyControlFeature",
                "IHttpResetFeature"
            };

            var allFeatures = alwaysFeatures
                .Concat(commonFeatures)
                .Concat(sometimesFeatures)
                .ToArray();

            // NOTE: This list MUST always match the set of feature interfaces implemented by HttpProtocol.
            // See also: src/Kestrel.Core/Internal/Http/HttpProtocol.FeatureCollection.cs
            var implementedFeatures = new[]
            {
                "IHttpRequestFeature",
                "IHttpResponseFeature",
                "IHttpResponseBodyFeature",
                "IRequestBodyPipeFeature",
                "IHttpUpgradeFeature",
                "IHttpRequestIdentifierFeature",
                "IHttpRequestLifetimeFeature",
                "IHttpRequestTrailersFeature",
                "IHttpConnectionFeature",
                "IHttpMaxRequestBodySizeFeature",
                "IHttpMinRequestBodyDataRateFeature",
                "IHttpBodyControlFeature",
                "IRouteValuesFeature",
                "IEndpointFeature"
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
                extraUsings: usings,
                fallbackFeatures: "ConnectionFeatures");
        }
    }
}
