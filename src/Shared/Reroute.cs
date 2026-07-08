// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal static class RerouteHelper
{
    internal const string GlobalRouteBuilderKey = "__GlobalEndpointRouteBuilder";
    internal const string UseRoutingKey = "__UseRouting";

    // Keep in sync with Microsoft.AspNetCore.Http.MiddlewareInvokedKeys.PostRoutingPipeline. It is duplicated as a
    // literal here because this shared source file is compiled into assemblies that don't all include
    // MiddlewareInvokedKeys.cs (matching how GlobalRouteBuilderKey/UseRoutingKey are duplicated above).
    private const string PostRoutingPipelineKey = "__Internal_PostRoutingPipeline";

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

            // Building this re-execution branch composes an EndpointRoutingMiddleware that consumes the framework's
            // single-use post-routing middleware slot (the implicit authentication/authorization/CSRF pipeline added
            // by WebApplicationBuilder) from the shared global route builder's properties. Capture it beforehand and
            // restore it afterwards so the primary request pipeline's routing still runs that implicit middleware.
            // Without this, a normal request to an antiforgery-required endpoint in an app that also re-executes
            // (e.g. UseStatusCodePagesWithReExecute in the Blazor Web template) fails with
            // "a middleware was not found that supports anti-forgery". See https://github.com/dotnet/aspnetcore/issues/67628.
            var routeBuilderProperties = (routeBuilder as IApplicationBuilder)?.Properties;
            object? postRoutingBlock = null;
            routeBuilderProperties?.TryGetValue(PostRoutingPipelineKey, out postRoutingBlock);

            var reroutePipeline = builder.Build();

            if (postRoutingBlock is not null && routeBuilderProperties is not null)
            {
                routeBuilderProperties[PostRoutingPipelineKey] = postRoutingBlock;
            }

            return reroutePipeline;
        }

        return next;
    }
}
