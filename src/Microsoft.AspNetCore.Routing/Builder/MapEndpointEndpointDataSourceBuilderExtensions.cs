// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Builder
{
    public static class MapEndpointEndpointDataSourceBuilderExtensions
    {
        public static MatcherEndpointBuilder MapEndpoint(
            this EndpointDataSourceBuilder builder,
            Func<RequestDelegate, RequestDelegate> invoker,
            string pattern,
            string displayName)
        {
            return MapEndpoint(builder, invoker, pattern, displayName, metadata: null);
        }

        public static MatcherEndpointBuilder MapEndpoint(
            this EndpointDataSourceBuilder builder,
            Func<RequestDelegate, RequestDelegate> invoker,
            RoutePattern pattern,
            string displayName)
        {
            return MapEndpoint(builder, invoker, pattern, displayName, metadata: null);
        }

        public static MatcherEndpointBuilder MapEndpoint(
            this EndpointDataSourceBuilder builder,
            Func<RequestDelegate, RequestDelegate> invoker,
            string pattern,
            string displayName,
            IList<object> metadata)
        {
            return MapEndpoint(builder, invoker, RoutePatternFactory.Parse(pattern), displayName, metadata);
        }

        public static MatcherEndpointBuilder MapEndpoint(
            this EndpointDataSourceBuilder builder,
            Func<RequestDelegate, RequestDelegate> invoker,
            RoutePattern pattern,
            string displayName,
            IList<object> metadata)
        {
            const int defaultOrder = 0;

            var endpointBuilder = new MatcherEndpointBuilder(
               invoker,
               pattern,
               defaultOrder);
            endpointBuilder.DisplayName = displayName;
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    endpointBuilder.Metadata.Add(item);
                }
            }

            builder.Endpoints.Add(endpointBuilder);
            return endpointBuilder;
        }
    }
}
