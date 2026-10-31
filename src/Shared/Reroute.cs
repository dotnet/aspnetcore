// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal static class RerouteHelper
{
    internal const string GlobalRouteBuilderKey = "__GlobalEndpointRouteBuilder";
    internal const string UseRoutingKey = "__UseRouting";

    internal static RequestDelegate Reroute(IApplicationBuilder app, object routeBuilder, RequestDelegate next)
    {
        if (app.Properties.TryGetValue(UseRoutingKey, out var useRouting) && useRouting is Func<IApplicationBuilder, IApplicationBuilder> useRoutingFunc)
        {
            var builder = app.New();
            // use the old routing pipeline if it exists so we preserve all the routes and matching logic
            // ((IApplicationBuilder)WebApplication).New() does not copy GlobalRouteBuilderKey automatically like it does for all other properties.
            builder.Properties[GlobalRouteBuilderKey] = routeBuilder;

            // UseRouting()
            useRoutingFunc(builder);

            // apply the next middleware
            builder.Run(next);

            return builder.Build();
        }

        return next;
    }
}
