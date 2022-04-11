// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

public static class OpenApiRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithApiDescription(this RouteHandlerBuilder builder, Action<OpenApiPathItem>? configureDescription = null)
    {
        builder.Add(endpointBuilder =>
        {
            if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder)
            {
                var pattern = routeEndpointBuilder.RoutePattern;
                var metadata = new EndpointMetadataCollection(routeEndpointBuilder.Metadata);
                var methodInfo = metadata.OfType<MethodInfo>().SingleOrDefault();
                var serviceProvider = routeEndpointBuilder.Metadata.OfType<IServiceProvider>().SingleOrDefault();
                if (methodInfo != null && serviceProvider != null)
                {
                    var generator = serviceProvider.GetService<OpenApiGenerator>();
                    var openApiPathItem = generator?.GetOpenApiPathItem(methodInfo, metadata, pattern);
                    if (configureDescription is not null)
                    {
                        configureDescription(openApiPathItem);
                    }
                    endpointBuilder.Metadata.Add(openApiPathItem);
                }  
            };
        });
        return builder;
    }
}
