// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal static class ReRouteHelper
{
    internal static RequestDelegate ReRoute(IApplicationBuilder app, object routeBuilder, RequestDelegate next)
    {
        const string globalRouteBuilderKey = "__GlobalEndpointRouteBuilder";
        const string useRoutingKey = "__UseRouting";

        var builder = app.New();
        // use the old routing pipeline if it exists so we preserve all the routes and matching logic
        // ((IApplicationBuilder)WebApplication).New() does not copy globalRouteBuilderKey automatically like it does for all other properties.
        builder.Properties[globalRouteBuilderKey] = routeBuilder;
        // UseRouting()
        if (builder.Properties.TryGetValue(useRoutingKey, out var useRouting) && useRouting is Func<IApplicationBuilder, IApplicationBuilder> useRoutingFunc)
        {
            useRoutingFunc(builder);
        }
        // apply the next middleware
        builder.Run(next);

        return builder.Build();
    }
}
